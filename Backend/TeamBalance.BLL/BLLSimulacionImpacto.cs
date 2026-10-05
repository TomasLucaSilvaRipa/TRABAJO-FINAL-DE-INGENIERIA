using System.Text.Json;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLSimulacionImpacto
{
    private readonly MPPSimulacionImpacto _simulacionMPP;
    private readonly MPPTarea _tareaMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLSimulacionImpacto(MPPSimulacionImpacto simulacionMPP, MPPTarea tareaMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _simulacionMPP = simulacionMPP;
        _tareaMPP = tareaMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<Tarea> ConsultarTareasDisponibles(Usuario usuario)
    {
        try
        {
            ValidarPermiso(usuario);

            return _tareaMPP.ConsultarPorPM(usuario)
                .Where(tarea => tarea.Activo
                    && tarea.HorasEstimadas > 0
                    && tarea.Deadline.HasValue
                    && !string.Equals(tarea.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(tarea.Estado, "Finalizado", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public SimulacionImpacto Calcular(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            ValidarSimulacion(simulacion, usuario);

            SimulacionImpacto datosOperativos = _simulacionMPP.ConsultarDatosOperativos(simulacion, usuario);
            decimal capacidadPorAusencias = Math.Max(0, datosOperativos.CapacidadSemanal - datosOperativos.CapacidadSemanal / 5 * datosOperativos.DiasAusenciaProximaSemana);
            decimal cargaActual = datosOperativos.CargaActual ?? 0;
            decimal cargaProyectada = cargaActual + datosOperativos.HorasTarea;
            decimal disponibilidadRestante = capacidadPorAusencias - cargaProyectada;
            decimal ocupacionActual = capacidadPorAusencias == 0 ? 0 : Math.Round(cargaActual * 100 / capacidadPorAusencias, 2);
            decimal ocupacionProyectada = capacidadPorAusencias == 0 ? 100 : Math.Round(cargaProyectada * 100 / capacidadPorAusencias, 2);
            List<string> advertencias = CrearAdvertencias(datosOperativos, capacidadPorAusencias, disponibilidadRestante, ocupacionProyectada);

            datosOperativos.CargaActual = cargaActual;
            datosOperativos.CargaProyectada = cargaProyectada;
            datosOperativos.DisponibilidadRestante = disponibilidadRestante;
            datosOperativos.PorcentajeOcupacionActual = ocupacionActual;
            datosOperativos.PorcentajeOcupacionProyectado = ocupacionProyectada;
            datosOperativos.GeneraSobrecarga = disponibilidadRestante < 0;
            datosOperativos.AdvertenciasJson = JsonSerializer.Serialize(advertencias);
            datosOperativos.ImpactoOperativo = datosOperativos.GeneraSobrecarga
                ? "Sobrecarga"
                : ocupacionProyectada >= 80 || datosOperativos.DiasAusenciaProximaSemana > 0
                    ? "Capacidad comprometida"
                    : "Viable";
            datosOperativos.FechaCreacion = DateTime.Now;
            datosOperativos.FechaExpiracion = DateTime.Now.AddDays(1);
            datosOperativos.Activo = true;

            return datosOperativos;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public SimulacionImpacto Conservar(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            SimulacionImpacto resultado = Calcular(simulacion, usuario);
            resultado = _simulacionMPP.Registrar(resultado, usuario);

            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SimulacionImpacto", resultado.ID,
                "Simular impacto de asignación", "Se conservó un escenario de simulación para la tarea seleccionada.",
                "Exitoso", "Información", "Planificación"));

            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public bool Descartar(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            ValidarPermiso(usuario);

            if (simulacion.ID > 0 && !_simulacionMPP.Descartar(simulacion, usuario))
            {
                throw new ArgumentException("No se encontró el escenario a descartar.");
            }

            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SimulacionImpacto", simulacion.ID > 0 ? simulacion.ID : null,
                "Descartar simulación de impacto", "El PM descartó un escenario sin modificar la asignación de la tarea.",
                "Exitoso", "Información", "Planificación"));

            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    private void ValidarSimulacion(SimulacionImpacto simulacion, Usuario usuario)
    {
        ValidarPermiso(usuario);

        if (simulacion.IdTarea <= 0 || simulacion.IdEmpleadoCandidato <= 0)
        {
            throw new ArgumentException("Seleccioná una tarea pendiente y un recurso candidato.");
        }
    }

    private void ValidarPermiso(Usuario usuario)
    {
        if (!_rolBLL.TienePermiso(usuario, "SimularImpacto"))
        {
            throw new UnauthorizedAccessException("No tenés permiso para simular impacto.");
        }
    }

    private static List<string> CrearAdvertencias(SimulacionImpacto simulacion, decimal capacidadOperativa, decimal disponibilidadRestante, decimal ocupacionProyectada)
    {
        List<string> advertencias = new List<string>();

        if (simulacion.DiasAusenciaProximaSemana > 0)
        {
            advertencias.Add($"El empleado tiene {simulacion.DiasAusenciaProximaSemana} día(s) de ausencia aprobada durante la próxima semana.");
        }

        if (disponibilidadRestante < 0)
        {
            advertencias.Add("La asignación supera la disponibilidad operativa semanal del empleado.");
        }
        else if (ocupacionProyectada >= 80)
        {
            advertencias.Add("La asignación deja al empleado con una ocupación igual o superior al 80%.");
        }

        if (simulacion.DeadlineTarea.HasValue)
        {
            int diasHastaDeadline = (simulacion.DeadlineTarea.Value.Date - DateTime.Today).Days;

            if (diasHastaDeadline < 0)
            {
                advertencias.Add("El deadline de la tarea ya venció.");
            }
            else if (diasHastaDeadline <= 7 && simulacion.HorasTarea > capacidadOperativa)
            {
                advertencias.Add("Las horas estimadas no entran en la capacidad disponible antes del deadline próximo.");
            }
        }

        return advertencias;
    }
}
