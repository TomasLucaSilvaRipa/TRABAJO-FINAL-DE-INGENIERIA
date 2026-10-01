using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRiesgoRetraso
{
    private readonly MPPRiesgoRetraso _riesgoMPP;
    private readonly BLLRol _rolBLL;

    public BLLRiesgoRetraso(MPPRiesgoRetraso riesgoMPP, BLLRol rolBLL)
    {
        _riesgoMPP = riesgoMPP;
        _rolBLL = rolBLL;
    }

    public ResultadoRiesgoRetraso ConsultarRiesgo(FiltroRiesgoRetraso filtro, Usuario usuario)
    {
        try
        {
            ValidarAcceso(usuario);
            List<Proyecto> proyectos = _riesgoMPP.ConsultarProyectosActivos(usuario, filtro);
            List<DetalleRiesgoProyecto> detalles = new List<DetalleRiesgoProyecto>();

            foreach (Proyecto proyecto in proyectos)
            {
                DetalleRiesgoProyecto detalle = CrearDetalle(proyecto);
                if (CumpleFiltro(detalle, filtro)) { detalles.Add(detalle); }
            }

            List<DetalleRiesgoProyecto> detallesOrdenados = detalles.OrderByDescending(detalle => ObtenerOrdenRiesgo(detalle.NivelRiesgo)).ThenByDescending(detalle => detalle.DiasPosibleRetraso).ToList();
            ResultadoRiesgoRetraso resultado = new ResultadoRiesgoRetraso(detallesOrdenados);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private DetalleRiesgoProyecto CrearDetalle(Proyecto proyecto)
    {
        List<Tarea> tareas = _riesgoMPP.ConsultarTareasProyecto(proyecto);
        List<Empleado> equipo = _riesgoMPP.ConsultarEquipoProyecto(proyecto);
        List<RegistroHora> registros = _riesgoMPP.ConsultarRegistrosHora(proyecto);
        decimal horasRestantes = CalcularHorasRestantes(tareas, registros);
        decimal disponibilidadEquipo = CalcularDisponibilidadEquipo(equipo);
        decimal ritmoAvance = CalcularRitmoAvance(proyecto, registros);
        DateTime? fechaEstimada = CalcularFechaEstimada(horasRestantes, disponibilidadEquipo, ritmoAvance);
        int diasRetraso = CalcularDiasRetraso(fechaEstimada, proyecto.Deadline);
        int tareasBloqueadas = tareas.Count(tarea => tarea.Bloqueada || string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase));
        string nivelRiesgo = ClasificarRiesgo(diasRetraso, tareasBloqueadas, horasRestantes, disponibilidadEquipo);
        List<CausaRiesgo> causas = IdentificarCausas(proyecto, tareas, horasRestantes, disponibilidadEquipo, ritmoAvance, tareasBloqueadas);
        string nombreCliente = string.IsNullOrWhiteSpace(proyecto.NombreCliente) ? "Cliente #" + proyecto.IdCliente : proyecto.NombreCliente;
        string nombrePM = string.IsNullOrWhiteSpace(proyecto.NombrePMResponsable) ? "PM #" + proyecto.IdPMResponsable : proyecto.NombrePMResponsable;
        DetalleRiesgoProyecto detalle = new DetalleRiesgoProyecto(proyecto.ID, proyecto.Nombre, nombreCliente, nombrePM, proyecto.Deadline, fechaEstimada, diasRetraso, horasRestantes, disponibilidadEquipo, ritmoAvance, nivelRiesgo, causas);
        return detalle;
    }

    private static decimal CalcularHorasRestantes(List<Tarea> tareas, List<RegistroHora> registros)
    {
        decimal horasEstimadas = tareas.Where(tarea => tarea.Activo).Sum(tarea => tarea.HorasEstimadas);
        decimal horasRegistradas = registros.Where(registro => registro.Activo).Sum(registro => registro.CantidadHoras);
        decimal horasRestantes = Math.Max(0, horasEstimadas - horasRegistradas);
        return horasRestantes;
    }

    private static decimal CalcularDisponibilidadEquipo(List<Empleado> equipo)
    {
        decimal disponibilidadEquipo = equipo.Where(empleado => empleado.Activo).Sum(empleado => empleado.HorasDisponiblesSemanales);
        return disponibilidadEquipo;
    }

    private static decimal CalcularRitmoAvance(Proyecto proyecto, List<RegistroHora> registros)
    {
        decimal horasRegistradas = registros.Where(registro => registro.Activo).Sum(registro => registro.CantidadHoras);
        DateTime fechaInicio = proyecto.FechaInicio ?? proyecto.FechaAlta;
        decimal semanasTranscurridas = Math.Max(1, Convert.ToDecimal((DateTime.Today - fechaInicio.Date).TotalDays / 7));
        decimal ritmoAvance = horasRegistradas / semanasTranscurridas;
        return Math.Round(ritmoAvance, 2);
    }

    private static DateTime? CalcularFechaEstimada(decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance)
    {
        if (horasRestantes <= 0) { return DateTime.Today; }
        if (disponibilidadEquipo <= 0) { return null; }

        decimal capacidadSemanal = ritmoAvance > 0 ? Math.Min(disponibilidadEquipo, ritmoAvance) : disponibilidadEquipo;
        decimal semanasNecesarias = horasRestantes / capacidadSemanal;
        DateTime fechaEstimada = DateTime.Today.AddDays(Convert.ToDouble(Math.Ceiling(semanasNecesarias * 7)));
        return fechaEstimada;
    }

    private static int CalcularDiasRetraso(DateTime? fechaEstimada, DateTime? deadline)
    {
        if (!fechaEstimada.HasValue || !deadline.HasValue) { return 0; }
        int diasRetraso = Math.Max(0, (fechaEstimada.Value.Date - deadline.Value.Date).Days);
        return diasRetraso;
    }

    private static string ClasificarRiesgo(int diasRetraso, int tareasBloqueadas, decimal horasRestantes, decimal disponibilidadEquipo)
    {
        if (tareasBloqueadas > 0 || disponibilidadEquipo <= 0 && horasRestantes > 0 || diasRetraso > 14) { return "Alto"; }
        if (diasRetraso > 0) { return "Medio"; }
        return "Bajo";
    }

    private static List<CausaRiesgo> IdentificarCausas(Proyecto proyecto, List<Tarea> tareas, decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance, int tareasBloqueadas)
    {
        List<CausaRiesgo> causas = new List<CausaRiesgo>();
        if (!proyecto.Deadline.HasValue) { causas.Add(new CausaRiesgo(proyecto.ID, "Proyecto", "El proyecto no tiene una fecha límite definida.", "Medio")); }
        if (tareasBloqueadas > 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Tarea", "Hay " + tareasBloqueadas + " tarea(s) bloqueada(s) que requieren seguimiento.", "Alto")); }
        if (horasRestantes > 0 && disponibilidadEquipo <= 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Equipo", "No se registró disponibilidad para el equipo asignado.", "Alto")); }
        if (horasRestantes > 0 && ritmoAvance <= 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Avance", "No hay horas registradas para estimar el ritmo de avance.", "Medio")); }
        if (causas.Count == 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Proyecto", "El proyecto no presenta señales críticas con la información disponible.", "Bajo")); }
        return causas;
    }

    private static bool CumpleFiltro(DetalleRiesgoProyecto detalle, FiltroRiesgoRetraso filtro)
    {
        bool coincideNivel = string.IsNullOrWhiteSpace(filtro.NivelRiesgo) || string.Equals(detalle.NivelRiesgo, filtro.NivelRiesgo, StringComparison.OrdinalIgnoreCase);
        bool coincideDeadline = !filtro.DiasHastaDeadlineMaximo.HasValue || !detalle.Deadline.HasValue || (detalle.Deadline.Value.Date - DateTime.Today).Days <= filtro.DiasHastaDeadlineMaximo.Value;
        return coincideNivel && coincideDeadline;
    }

    private static int ObtenerOrdenRiesgo(string nivelRiesgo)
    {
        if (string.Equals(nivelRiesgo, "Alto", StringComparison.OrdinalIgnoreCase)) { return 3; }
        if (string.Equals(nivelRiesgo, "Medio", StringComparison.OrdinalIgnoreCase)) { return 2; }
        return 1;
    }

    private void ValidarAcceso(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "ConsultarTableroEjecutivo")) { throw new UnauthorizedAccessException("No tenés permiso para consultar el riesgo de retraso."); }
    }
}
