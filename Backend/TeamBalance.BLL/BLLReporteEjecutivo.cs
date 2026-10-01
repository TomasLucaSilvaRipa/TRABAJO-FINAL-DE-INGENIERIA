using TeamBalance.BE.Entidades;

namespace TeamBalance.BLL;

public class BLLReporteEjecutivo
{
    private readonly BLLRiesgoRetraso _riesgoBLL;
    private readonly BLLRol _rolBLL;

    public BLLReporteEjecutivo(BLLRiesgoRetraso riesgoBLL, BLLRol rolBLL)
    {
        _riesgoBLL = riesgoBLL;
        _rolBLL = rolBLL;
    }

    public ReporteEjecutivo PrevisualizarReporte(ConfiguracionReporteEjecutivo configuracion, Usuario usuario)
    {
        try
        {
            ValidarAcceso(usuario);
            ValidarConfiguracion(configuracion);
            FiltroRiesgoRetraso filtroRiesgo = new FiltroRiesgoRetraso(configuracion.Filtro.IdProyecto, configuracion.Filtro.IdCliente, configuracion.Filtro.IdResponsable, null, null);
            ResultadoRiesgoRetraso resultadoRiesgo = _riesgoBLL.ConsultarRiesgo(filtroRiesgo, usuario);
            List<SeccionReporteEjecutivo> secciones = new List<SeccionReporteEjecutivo>();

            if (configuracion.IncluirTodasLasSecciones || configuracion.IncluirRiesgoRetraso)
            {
                SeccionReporteEjecutivo seccionRiesgo = GenerarSeccionRiesgoRetraso(resultadoRiesgo, secciones.Count + 1);
                secciones.Add(seccionRiesgo);
            }

            if (configuracion.IncluirTodasLasSecciones || configuracion.IncluirDesvioHoras)
            {
                SeccionReporteEjecutivo seccionDesvio = GenerarSeccionDesvioHoras(resultadoRiesgo, secciones.Count + 1);
                secciones.Add(seccionDesvio);
            }

            if (configuracion.IncluirTodasLasSecciones || configuracion.IncluirOcupacionOperativa)
            {
                SeccionReporteEjecutivo seccionOcupacion = GenerarSeccionOcupacion(resultadoRiesgo, secciones.Count + 1);
                secciones.Add(seccionOcupacion);
            }

            if (configuracion.IncluirTodasLasSecciones || configuracion.IncluirEficienciaPerfiles || configuracion.IncluirConvenienciaOperativa)
            {
                SeccionReporteEjecutivo seccionDisponibilidad = GenerarSeccionDisponibilidad(resultadoRiesgo, secciones.Count + 1);
                secciones.Add(seccionDisponibilidad);
            }

            string titulo = string.IsNullOrWhiteSpace(configuracion.Titulo) ? "Reporte ejecutivo de gestión" : configuracion.Titulo.Trim();
            ReporteEjecutivo reporte = new ReporteEjecutivo(0, titulo, configuracion.Filtro.FechaDesde, configuracion.Filtro.FechaHasta, DateTime.Now, secciones);
            return reporte;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static SeccionReporteEjecutivo GenerarSeccionRiesgoRetraso(ResultadoRiesgoRetraso resultado, int orden)
    {
        decimal alto = resultado.Proyectos.Count(proyecto => string.Equals(proyecto.NivelRiesgo, "Alto", StringComparison.OrdinalIgnoreCase));
        decimal medio = resultado.Proyectos.Count(proyecto => string.Equals(proyecto.NivelRiesgo, "Medio", StringComparison.OrdinalIgnoreCase));
        decimal bajo = resultado.Proyectos.Count(proyecto => string.Equals(proyecto.NivelRiesgo, "Bajo", StringComparison.OrdinalIgnoreCase));
        List<IndicadorReporteEjecutivo> indicadores = new List<IndicadorReporteEjecutivo>();
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos en riesgo alto", alto, "proyectos", alto > 0 ? "Alto" : "Bajo", "Proyectos con bloqueos, capacidad insuficiente o demora estimada mayor a dos semanas."));
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos en riesgo medio", medio, "proyectos", medio > 0 ? "Medio" : "Bajo", "Proyectos con demora estimada que requieren seguimiento."));
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos sin riesgo crítico", bajo, "proyectos", "Bajo", "Proyectos que no presentan demora estimada con la información actual."));
        SeccionReporteEjecutivo seccion = new SeccionReporteEjecutivo("Riesgo de retraso", "Síntesis de los plazos estimados y los factores que pueden afectar su cumplimiento.", orden, indicadores);
        return seccion;
    }

    private static SeccionReporteEjecutivo GenerarSeccionDesvioHoras(ResultadoRiesgoRetraso resultado, int orden)
    {
        decimal horasRestantes = resultado.Proyectos.Sum(proyecto => proyecto.HorasRestantes);
        decimal proyectosConHorasPendientes = resultado.Proyectos.Count(proyecto => proyecto.HorasRestantes > 0);
        List<IndicadorReporteEjecutivo> indicadores = new List<IndicadorReporteEjecutivo>();
        indicadores.Add(new IndicadorReporteEjecutivo("Horas estimadas pendientes", horasRestantes, "horas", horasRestantes > 0 ? "Medio" : "Bajo", "Horas aún necesarias para finalizar las tareas activas relevadas."));
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos con trabajo pendiente", proyectosConHorasPendientes, "proyectos", proyectosConHorasPendientes > 0 ? "Medio" : "Bajo", "Proyectos que todavía concentran horas estimadas sin registrar."));
        SeccionReporteEjecutivo seccion = new SeccionReporteEjecutivo("Desvío y carga pendiente", "Lectura consolidada de la carga de trabajo restante por proyecto.", orden, indicadores);
        return seccion;
    }

    private static SeccionReporteEjecutivo GenerarSeccionOcupacion(ResultadoRiesgoRetraso resultado, int orden)
    {
        decimal disponibilidad = resultado.Proyectos.Sum(proyecto => proyecto.DisponibilidadEquipo);
        decimal proyectosSinCapacidad = resultado.Proyectos.Count(proyecto => proyecto.HorasRestantes > 0 && proyecto.DisponibilidadEquipo <= 0);
        List<IndicadorReporteEjecutivo> indicadores = new List<IndicadorReporteEjecutivo>();
        indicadores.Add(new IndicadorReporteEjecutivo("Disponibilidad semanal relevada", disponibilidad, "horas/semana", disponibilidad > 0 ? "Bajo" : "Alto", "Capacidad de los equipos asignados a los proyectos incluidos."));
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos sin capacidad informada", proyectosSinCapacidad, "proyectos", proyectosSinCapacidad > 0 ? "Alto" : "Bajo", "Proyectos con trabajo pendiente y sin disponibilidad del equipo para estimarlo."));
        SeccionReporteEjecutivo seccion = new SeccionReporteEjecutivo("Ocupación operativa", "Disponibilidad declarada por los equipos involucrados en la ejecución.", orden, indicadores);
        return seccion;
    }

    private static SeccionReporteEjecutivo GenerarSeccionDisponibilidad(ResultadoRiesgoRetraso resultado, int orden)
    {
        decimal ritmoPromedio = resultado.Proyectos.Count == 0 ? 0 : Math.Round(resultado.Proyectos.Average(proyecto => proyecto.RitmoAvance), 2);
        decimal proyectosAnalizados = resultado.Proyectos.Count;
        List<IndicadorReporteEjecutivo> indicadores = new List<IndicadorReporteEjecutivo>();
        indicadores.Add(new IndicadorReporteEjecutivo("Ritmo promedio", ritmoPromedio, "horas/semana", ritmoPromedio > 0 ? "Bajo" : "Medio", "Promedio de horas registradas semanalmente desde el inicio de los proyectos."));
        indicadores.Add(new IndicadorReporteEjecutivo("Proyectos analizados", proyectosAnalizados, "proyectos", "Bajo", "Alcance utilizado para obtener los indicadores de esta previsualización."));
        SeccionReporteEjecutivo seccion = new SeccionReporteEjecutivo("Capacidad y eficiencia", "Indicadores de ritmo de ejecución disponibles con los registros actuales.", orden, indicadores);
        return seccion;
    }

    private void ValidarAcceso(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "ConsultarTableroEjecutivo")) { throw new UnauthorizedAccessException("No tenés permiso para generar reportes ejecutivos."); }
    }

    private static void ValidarConfiguracion(ConfiguracionReporteEjecutivo configuracion)
    {
        if (configuracion.Filtro is null) { throw new ArgumentException("Definí los filtros del reporte."); }
        bool noHaySecciones = !configuracion.IncluirTodasLasSecciones && !configuracion.IncluirOcupacionOperativa && !configuracion.IncluirRiesgoRetraso && !configuracion.IncluirDesvioHoras && !configuracion.IncluirEficienciaPerfiles && !configuracion.IncluirConvenienciaOperativa;
        if (noHaySecciones) { throw new ArgumentException("Seleccioná al menos una sección para el reporte."); }
    }
}
