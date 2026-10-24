using TeamBalance.BE.Entidades;

namespace TeamBalance.BLL;

public class MotorCargaOperativa
{
    public CargaOperativaEmpleado Calcular(Empleado empleado, IEnumerable<Tarea> tareas, IEnumerable<RegistroHora> registros, IEnumerable<AusenciaEmpleado> ausencias, DateTime desde, DateTime hasta)
        => Calcular(empleado, tareas, Array.Empty<AsignacionTarea>(), registros, ausencias, desde, hasta);

    public CargaOperativaEmpleado Calcular(Empleado empleado, IEnumerable<Tarea> tareas, IEnumerable<AsignacionTarea> asignaciones, IEnumerable<RegistroHora> registros, IEnumerable<AusenciaEmpleado> ausencias, DateTime desde, DateTime hasta)
    {
        DateTime inicioPeriodo = desde.Date;
        DateTime finPeriodo = hasta.Date;
        CargaOperativaEmpleado resultado = new CargaOperativaEmpleado();
        resultado.IdEmpleado = empleado.ID;
        Dictionary<DateTime, decimal> capacidadPorDia = CrearCapacidadPorDia(empleado.HorasDisponiblesSemanales, ausencias, inicioPeriodo, finPeriodo);
        Dictionary<DateTime, decimal> cargaPorDia = capacidadPorDia.Keys.ToDictionary(fecha => fecha, _ => 0m);
        resultado.HorasCapacidadPeriodo = capacidadPorDia.Values.Sum();
        resultado.HorasRegistradasPeriodo = registros
            .Where(registro => registro.IdEmpleado == empleado.ID && registro.Activo && registro.Fecha.Date >= inicioPeriodo && registro.Fecha.Date <= finPeriodo)
            .Sum(registro => registro.CantidadHoras);

        foreach (Tarea tarea in tareas.Where(EsPlanificable))
        {
            List<AsignacionTarea> asignacionesTarea = asignaciones.Where(asignacion => asignacion.IdTarea == tarea.ID && asignacion.IdEmpleado == empleado.ID && asignacion.Activa && (!asignacion.FechaHasta.HasValue || asignacion.FechaHasta.Value.Date >= inicioPeriodo)).ToList();
            // Las tareas históricas que todavía no fueron migradas continúan usando su responsable principal.
            bool usaAsignacionLegada = asignacionesTarea.Count == 0 && !asignaciones.Any(asignacion => asignacion.IdTarea == tarea.ID && asignacion.Activa) && tarea.IdEmpleadoAsignado == empleado.ID;
            if (!usaAsignacionLegada && asignacionesTarea.Count == 0) { continue; }

            decimal horasRestantes = CalcularHorasRestantes(tarea, registros);
            if (horasRestantes <= 0) { continue; }
            decimal horasPlanificadasEmpleado = usaAsignacionLegada
                ? horasRestantes
                : Math.Round(horasRestantes * asignacionesTarea.Sum(asignacion => asignacion.HorasAsignadas) / tarea.HorasEstimadas, 2);
            if (horasPlanificadasEmpleado <= 0) { continue; }

            // La ocupación pertenece al período consultado. Usar DateTime.Today
            // hacía que, durante el fin de semana, el inicio quedara después del
            // viernes que cierra la semana y todas las tareas se descartaran.
            DateTime inicioPlanificacion = Max(inicioPeriodo, tarea.FechaInicio!.Value.Date);
            DateTime finPlanificacion = tarea.Deadline!.Value.Date;
            if (finPlanificacion < inicioPlanificacion) { continue; }

            int diasLaborablesTarea = ContarDiasLaborables(inicioPlanificacion, finPlanificacion);
            int diasLaborablesEnPeriodo = ContarDiasLaborables(Max(inicioPlanificacion, inicioPeriodo), Min(finPlanificacion, finPeriodo));
            if (diasLaborablesTarea == 0 || diasLaborablesEnPeriodo == 0) { continue; }

            decimal horasPorDia = horasPlanificadasEmpleado / diasLaborablesTarea;
            decimal horasEnPeriodo = 0;
            for (DateTime fecha = Max(inicioPlanificacion, inicioPeriodo); fecha <= Min(finPlanificacion, finPeriodo); fecha = fecha.AddDays(1))
            {
                if (EsDiaLaborable(fecha))
                {
                    cargaPorDia[fecha] += horasPorDia;
                    horasEnPeriodo += horasPorDia;
                }
            }
            horasEnPeriodo = Math.Round(horasEnPeriodo, 2);
            resultado.HorasPlanificadasPorTarea[tarea.ID] = horasEnPeriodo;
            resultado.HorasPlanificadasPeriodo += horasEnPeriodo;
        }

        resultado.HorasPlanificadasPeriodo = Math.Round(resultado.HorasPlanificadasPeriodo, 2);
        resultado.MayorPorcentajeOcupacionDiaria = cargaPorDia.Count == 0 ? 0 : cargaPorDia.Max(par => capacidadPorDia[par.Key] <= 0 ? (par.Value > 0 ? 999m : 0m) : Math.Round(par.Value * 100 / capacidadPorDia[par.Key], 2));
        resultado.TieneSobrecargaDiaria = resultado.MayorPorcentajeOcupacionDiaria > 100;
        return resultado;
    }

    public (DateTime Desde, DateTime Hasta) ObtenerPeriodo(FiltroOcupacionOperativa filtro)
    {
        if (filtro.Desde.HasValue || filtro.Hasta.HasValue)
        {
            DateTime desde = filtro.Desde?.Date ?? InicioSemanaLaboral(filtro.Hasta!.Value);
            DateTime hasta = filtro.Hasta?.Date ?? FinSemanaLaboral(filtro.Desde!.Value);
            return (desde, hasta);
        }

        return (InicioSemanaLaboral(DateTime.Today), FinSemanaLaboral(DateTime.Today));
    }

    private static decimal CalcularHorasRestantes(Tarea tarea, IEnumerable<RegistroHora> registros)
    {
        decimal horasRegistradasTarea = registros.Where(registro => registro.IdTarea == tarea.ID && registro.Activo).Sum(registro => registro.CantidadHoras);
        decimal horasSegunAvance = tarea.HorasEstimadas * Math.Clamp(tarea.PorcentajeAvance, 0, 100) / 100m;
        return Math.Max(0, Math.Round(tarea.HorasEstimadas - Math.Max(horasRegistradasTarea, horasSegunAvance), 2));
    }

    private static Dictionary<DateTime, decimal> CrearCapacidadPorDia(decimal horasSemanales, IEnumerable<AusenciaEmpleado> ausencias, DateTime desde, DateTime hasta)
    {
        decimal capacidadBaseDiaria = Math.Max(0, horasSemanales) / 5m;
        Dictionary<DateTime, decimal> capacidad = new Dictionary<DateTime, decimal>();
        foreach (DateTime fecha in DiasLaborables(desde, hasta))
        {
            List<AusenciaEmpleado> ausenciasDelDia = ausencias.Where(ausencia => EsAusenciaAprobadaParaFecha(ausencia, fecha)).ToList();
            decimal noDisponible = ausenciasDelDia.Any(ausencia => !ausencia.HorasNoDisponiblesAprobadas.HasValue)
                ? capacidadBaseDiaria
                : ausenciasDelDia.Sum(HorasNoDisponiblesPorDia);
            capacidad[fecha] = Math.Max(0, Math.Round(capacidadBaseDiaria - noDisponible, 2));
        }
        return capacidad;
    }

    private static bool EsPlanificable(Tarea tarea)
    {
        return tarea.Activo
            && tarea.HorasEstimadas > 0
            && tarea.FechaInicio.HasValue
            && tarea.Deadline.HasValue
            && tarea.FechaFinReal is null
            && !EsFinalizada(tarea.Estado);
    }

    private static bool EsFinalizada(string? estado)
    {
        return string.Equals(estado, "Finalizada", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "Finalizado", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "Completada", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "Completado", StringComparison.OrdinalIgnoreCase);
    }

    private static int ContarDiasLaborables(DateTime desde, DateTime hasta)
    {
        if (hasta < desde) { return 0; }
        int cantidad = 0;
        for (DateTime fecha = desde.Date; fecha <= hasta.Date; fecha = fecha.AddDays(1))
        {
            if (EsDiaLaborable(fecha)) { cantidad++; }
        }
        return cantidad;
    }

    private static IEnumerable<DateTime> DiasLaborables(DateTime desde, DateTime hasta)
    {
        for (DateTime fecha = desde.Date; fecha <= hasta.Date; fecha = fecha.AddDays(1))
        {
            if (EsDiaLaborable(fecha)) { yield return fecha; }
        }
    }

    private static bool EsDiaLaborable(DateTime fecha) => fecha.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    private static bool EsAusenciaAprobadaParaFecha(AusenciaEmpleado ausencia, DateTime fecha)
    {
        return ausencia.Activo
            && string.Equals(ausencia.Estado, "Aprobada", StringComparison.OrdinalIgnoreCase)
            && ausencia.FechaInicioAprobada.HasValue
            && ausencia.FechaFinAprobada.HasValue
            && ausencia.FechaInicioAprobada.Value.Date <= fecha.Date
            && ausencia.FechaFinAprobada.Value.Date >= fecha.Date;
    }

    private static decimal HorasNoDisponiblesPorDia(AusenciaEmpleado ausencia)
    {
        int diasLaborablesAusencia = ContarDiasLaborables(ausencia.FechaInicioAprobada!.Value, ausencia.FechaFinAprobada!.Value);
        if (diasLaborablesAusencia == 0) { return 0; }
        return ausencia.HorasNoDisponiblesAprobadas.Value / diasLaborablesAusencia;
    }

    private static DateTime InicioSemanaLaboral(DateTime fecha)
    {
        int desplazamiento = ((int)fecha.DayOfWeek + 6) % 7;
        return fecha.Date.AddDays(-desplazamiento);
    }

    private static DateTime FinSemanaLaboral(DateTime fecha) => InicioSemanaLaboral(fecha).AddDays(4);
    private static DateTime Max(DateTime primero, DateTime segundo) => primero >= segundo ? primero : segundo;
    private static DateTime Min(DateTime primero, DateTime segundo) => primero <= segundo ? primero : segundo;
}
