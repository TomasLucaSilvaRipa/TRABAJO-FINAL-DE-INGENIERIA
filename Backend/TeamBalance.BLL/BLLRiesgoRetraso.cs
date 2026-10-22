using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLRiesgoRetraso
{
    private const int DiasVentanaRitmo = 28;
    private readonly MPPRiesgoRetraso _riesgoMPP;
    private readonly MPPAnaliticaOperativa _analiticaMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;

    public BLLRiesgoRetraso(MPPRiesgoRetraso riesgoMPP, MPPAnaliticaOperativa analiticaMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL, EmailService emailService)
    {
        _riesgoMPP = riesgoMPP;
        _analiticaMPP = analiticaMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _emailService = emailService;
    }

    public ResultadoRiesgoRetraso ConsultarRiesgo(FiltroRiesgoRetraso filtro, Usuario usuario)
    {
        try
        {
            ValidarAcceso(usuario);
            FiltroRiesgoRetraso filtroAplicado = CrearFiltroAplicado(filtro, usuario);
            ValidarFiltro(filtroAplicado);
            List<Proyecto> proyectos = _riesgoMPP.ConsultarProyectosActivos(usuario, filtroAplicado);
            List<DetalleRiesgoProyecto> detalles = new List<DetalleRiesgoProyecto>();

            foreach (Proyecto proyecto in proyectos)
            {
                DetalleRiesgoProyecto detalle = CrearDetalle(proyecto);
                if (CumpleFiltro(detalle, filtroAplicado)) { detalles.Add(detalle); }
            }

            List<DetalleRiesgoProyecto> detallesOrdenados = detalles.OrderByDescending(detalle => ObtenerOrdenRiesgo(detalle.NivelRiesgo)).ThenByDescending(detalle => detalle.DiasPosibleRetraso).ToList();
            ResultadoRiesgoRetraso resultado = new ResultadoRiesgoRetraso(detallesOrdenados);
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public async Task<string> RegistrarSeguimiento(Usuario usuario, NuevoSeguimientoRiesgoRetraso seguimiento)
    {
        ValidarAcceso(usuario);
        if (seguimiento.IdProyecto <= 0) { throw new ArgumentException("Seleccioná un proyecto para registrar el seguimiento."); }
        if (string.IsNullOrWhiteSpace(seguimiento.Observacion)) { throw new ArgumentException("Describí la acción de seguimiento."); }
        if (seguimiento.Observacion.Trim().Length > 1500) { throw new ArgumentException("La observación no puede superar los 1500 caracteres."); }

        FiltroRiesgoRetraso filtroProyecto = CrearFiltroAplicado(new FiltroRiesgoRetraso(seguimiento.IdProyecto, null, null, null, null), usuario);
        Proyecto proyecto = _riesgoMPP.ConsultarProyectosActivos(usuario, filtroProyecto).FirstOrDefault() ?? throw new KeyNotFoundException("El proyecto seleccionado no está disponible para tu usuario.");
        if (seguimiento.IdTarea.HasValue && !_riesgoMPP.ConsultarTareasProyecto(proyecto).Any(tarea => tarea.ID == seguimiento.IdTarea.Value)) { throw new ArgumentException("La tarea seleccionada no pertenece al proyecto indicado."); }

        seguimiento.Observacion = seguimiento.Observacion.Trim();
        string? tituloTarea = seguimiento.IdTarea.HasValue ? _riesgoMPP.ConsultarTareasProyecto(proyecto).First(tarea => tarea.ID == seguimiento.IdTarea.Value).Titulo : null;
        DestinatarioDesvioHoras? destinatarioResponsable = seguimiento.ComunicarResponsable ? _riesgoMPP.ConsultarDestinatarioSeguimiento(proyecto.ID, seguimiento.IdTarea) : null;
        if (seguimiento.ComunicarResponsable && (destinatarioResponsable is null || string.IsNullOrWhiteSpace(destinatarioResponsable.Email))) { throw new InvalidOperationException("No se encontró un correo activo para comunicar el seguimiento."); }
        int idSeguimiento = _riesgoMPP.RegistrarSeguimiento(usuario, seguimiento);
        SolicitudRevisionGestion solicitud = new SolicitudRevisionGestion
        {
            IdAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."), IdProyecto = proyecto.ID,
            IdTarea = seguimiento.IdTarea, IdPMResponsable = proyecto.IdPMResponsable, IdUsuarioSolicitante = usuario.ID,
            Tipo = "RiesgoRetraso", CausaPrincipal = "Se registró un seguimiento por riesgo de retraso.", AccionSugerida = seguimiento.Observacion
        };
        solicitud.ID = _analiticaMPP.RegistrarSolicitudRevisionGestion(solicitud);
        DestinatarioDesvioHoras? destinatarioPM = _analiticaMPP.ConsultarDestinatarioPM(solicitud.IdPMResponsable);
        bool correoPM = destinatarioPM is not null && !string.IsNullOrWhiteSpace(destinatarioPM.Email) && await _emailService.EnviarCorreoSolicitudRevisionGestion(destinatarioPM.Email, destinatarioPM.Nombre, "riesgo de retraso", proyecto.Nombre, tituloTarea, solicitud.CausaPrincipal, solicitud.AccionSugerida);
        bool correoResponsable = !seguimiento.ComunicarResponsable || (destinatarioResponsable!.IdUsuario == destinatarioPM?.IdUsuario || await _emailService.EnviarCorreoSeguimientoRiesgo(destinatarioResponsable.Email, destinatarioResponsable.Nombre, proyecto.Nombre, tituloTarea, seguimiento.Observacion));
        bool correosEnviados = correoPM && correoResponsable;
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SeguimientoRiesgoRetraso", idSeguimiento, "RegistrarSeguimientoRiesgo", "Se registró un seguimiento y una revisión pendiente para el PM del proyecto '" + proyecto.Nombre + "'.", correosEnviados ? "Exitoso" : "Parcial", correosEnviados ? "Informacion" : "Advertencia", "Proyectos"));
        return correoPM
            ? "El seguimiento quedó registrado, enviado al PM y disponible en sus revisiones pendientes."
            : "El seguimiento quedó registrado y disponible en las revisiones del PM, pero no se pudo enviar el correo al PM.";
    }

    private DetalleRiesgoProyecto CrearDetalle(Proyecto proyecto)
    {
        List<Tarea> tareas = _riesgoMPP.ConsultarTareasProyecto(proyecto);
        List<Empleado> equipo = _riesgoMPP.ConsultarEquipoProyecto(proyecto);
        List<RegistroHora> registros = _riesgoMPP.ConsultarRegistrosHora(proyecto);
        Dictionary<int, decimal> horasRegistradasPorTarea = registros.Where(registro => registro.Activo).GroupBy(registro => registro.IdTarea).ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(registro => registro.CantidadHoras));
        List<TareaRiesgoRetraso> tareasPendientes = CrearTareasPendientes(tareas, horasRegistradasPorTarea);
        decimal horasRestantes = tareasPendientes.Sum(tarea => tarea.HorasRestantes);
        decimal disponibilidadEquipo = CalcularDisponibilidadEquipo(equipo);
        decimal ritmoAvance = CalcularRitmoAvance(proyecto, registros);
        DateTime? fechaEstimada = CalcularFechaEstimada(horasRestantes, disponibilidadEquipo, ritmoAvance);
        int diasRetraso = CalcularDiasRetraso(fechaEstimada, proyecto.Deadline);
        int tareasBloqueadas = tareasPendientes.Count(tarea => tarea.Bloqueada || string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase));
        string nivelRiesgo = ClasificarRiesgo(diasRetraso, tareasBloqueadas, horasRestantes, disponibilidadEquipo, proyecto.Deadline);
        List<CausaRiesgo> causas = IdentificarCausas(proyecto, tareasPendientes, horasRestantes, disponibilidadEquipo, ritmoAvance, tareasBloqueadas, fechaEstimada, diasRetraso);
        string nombreCliente = string.IsNullOrWhiteSpace(proyecto.NombreCliente) ? "Cliente #" + proyecto.IdCliente : proyecto.NombreCliente;
        string nombrePM = string.IsNullOrWhiteSpace(proyecto.NombrePMResponsable) ? "PM #" + proyecto.IdPMResponsable : proyecto.NombrePMResponsable;
        DetalleRiesgoProyecto detalle = new DetalleRiesgoProyecto(proyecto.ID, proyecto.Nombre, nombreCliente, nombrePM, proyecto.Deadline, fechaEstimada, diasRetraso, horasRestantes, disponibilidadEquipo, ritmoAvance, nivelRiesgo, causas, tareasPendientes);
        return detalle;
    }

    private static List<TareaRiesgoRetraso> CrearTareasPendientes(IEnumerable<Tarea> tareas, IReadOnlyDictionary<int, decimal> horasRegistradasPorTarea)
    {
        return tareas.Where(tarea => tarea.Activo && !EsTareaFinalizada(tarea)).Select(tarea =>
        {
            decimal horasEstimadas = Math.Max(0, tarea.HorasEstimadas);
            decimal horasRegistradas = horasRegistradasPorTarea.GetValueOrDefault(tarea.ID);
            decimal porcentajeAvance = Math.Clamp(tarea.PorcentajeAvance, 0, 100);
            decimal horasCubiertasPorAvance = horasEstimadas * porcentajeAvance / 100;
            return new TareaRiesgoRetraso
            {
                IdTarea = tarea.ID,
                Titulo = tarea.Titulo,
                Estado = tarea.Estado ?? "Sin estado",
                Bloqueada = tarea.Bloqueada,
                HorasEstimadas = horasEstimadas,
                PorcentajeAvance = porcentajeAvance,
                HorasRestantes = Math.Max(0, horasEstimadas - Math.Max(horasRegistradas, horasCubiertasPorAvance))
            };
        }).Where(tarea => tarea.HorasRestantes > 0 || tarea.Bloqueada || string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase)).OrderByDescending(tarea => tarea.Bloqueada || string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase)).ThenByDescending(tarea => tarea.HorasRestantes).ToList();
    }

    private static decimal CalcularDisponibilidadEquipo(List<Empleado> equipo)
    {
        decimal disponibilidadEquipo = equipo.Where(empleado => empleado.Activo).Sum(empleado => empleado.HorasDisponiblesSemanales);
        return disponibilidadEquipo;
    }

    private static decimal CalcularRitmoAvance(Proyecto proyecto, List<RegistroHora> registros)
    {
        DateTime hoy = DateTime.Today;
        DateTime inicioVentana = hoy.AddDays(-(DiasVentanaRitmo - 1));
        DateTime fechaInicioProyecto = (proyecto.FechaInicio ?? proyecto.FechaAlta).Date;
        if (fechaInicioProyecto > inicioVentana) { inicioVentana = fechaInicioProyecto; }
        decimal horasRegistradas = registros.Where(registro => registro.Activo && registro.Fecha.Date >= inicioVentana && registro.Fecha.Date <= hoy).Sum(registro => registro.CantidadHoras);
        decimal semanasTranscurridas = Math.Max(1m / 7m, Convert.ToDecimal((hoy - inicioVentana).TotalDays + 1) / 7m);
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

    private static string ClasificarRiesgo(int diasRetraso, int tareasBloqueadas, decimal horasRestantes, decimal disponibilidadEquipo, DateTime? deadline)
    {
        if (tareasBloqueadas > 0 || disponibilidadEquipo <= 0 && horasRestantes > 0 || diasRetraso > 14) { return "Alto"; }
        if (diasRetraso > 0) { return "Medio"; }
        if (!deadline.HasValue && horasRestantes > 0) { return "Medio"; }
        return "Bajo";
    }

    private static List<CausaRiesgo> IdentificarCausas(Proyecto proyecto, List<TareaRiesgoRetraso> tareas, decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance, int tareasBloqueadas, DateTime? fechaEstimada, int diasRetraso)
    {
        List<CausaRiesgo> causas = new List<CausaRiesgo>();
        if (!proyecto.Deadline.HasValue) { causas.Add(new CausaRiesgo(proyecto.ID, "Proyecto", "El proyecto no tiene una fecha límite definida.", "Medio")); }
        if (tareasBloqueadas > 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Tarea", "Hay " + tareasBloqueadas + " tarea(s) bloqueada(s) que requieren seguimiento.", "Alto")); }
        if (horasRestantes > 0 && disponibilidadEquipo <= 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Equipo", "No se registró disponibilidad para el equipo asignado.", "Alto")); }
        if (horasRestantes > 0 && ritmoAvance <= 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Avance", "No hay avance registrado en los últimos " + DiasVentanaRitmo + " días; la fecha se proyecta con la capacidad disponible.", "Medio")); }
        if (diasRetraso > 0 && fechaEstimada.HasValue) { causas.Add(new CausaRiesgo(proyecto.ID, "Plazo", "Con el ritmo y la capacidad actuales, la finalización se proyecta para el " + fechaEstimada.Value.ToString("dd/MM/yyyy") + ", " + diasRetraso + " día(s) después del deadline.", diasRetraso > 14 ? "Alto" : "Medio")); }
        if (causas.Count == 0) { causas.Add(new CausaRiesgo(proyecto.ID, "Proyecto", "El proyecto no presenta señales críticas con la información disponible.", "Bajo")); }
        return causas;
    }

    private static bool CumpleFiltro(DetalleRiesgoProyecto detalle, FiltroRiesgoRetraso filtro)
    {
        bool coincideNivel = string.IsNullOrWhiteSpace(filtro.NivelRiesgo) || string.Equals(detalle.NivelRiesgo, filtro.NivelRiesgo, StringComparison.OrdinalIgnoreCase);
        bool coincideDeadline = !filtro.DiasHastaDeadlineMaximo.HasValue || (detalle.Deadline.HasValue && (detalle.Deadline.Value.Date - DateTime.Today).Days <= filtro.DiasHastaDeadlineMaximo.Value);
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

    private FiltroRiesgoRetraso CrearFiltroAplicado(FiltroRiesgoRetraso? filtro, Usuario usuario)
    {
        FiltroRiesgoRetraso filtroAplicado = filtro ?? new FiltroRiesgoRetraso();
        if (EsPMSinRolDueno(usuario))
        {
            filtroAplicado.IdPM = _analiticaMPP.ConsultarIdPM(usuario);
        }
        return filtroAplicado;
    }

    private static bool EsPMSinRolDueno(Usuario usuario)
    {
        bool esPM = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase));
        bool esDueno = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase));
        return esPM && !esDueno;
    }

    private static bool EsTareaFinalizada(Tarea tarea)
    {
        return tarea.FechaFinReal.HasValue || tarea.PorcentajeAvance >= 100 || string.Equals(tarea.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Finalizado", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Completada", StringComparison.OrdinalIgnoreCase) || string.Equals(tarea.Estado, "Completado", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidarFiltro(FiltroRiesgoRetraso filtro)
    {
        if (!string.IsNullOrWhiteSpace(filtro.NivelRiesgo) && !new[] { "Bajo", "Medio", "Alto" }.Contains(filtro.NivelRiesgo, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("El nivel de riesgo seleccionado no es válido."); }
        if (filtro.DiasHastaDeadlineMaximo.HasValue && filtro.DiasHastaDeadlineMaximo.Value < 0) { throw new ArgumentException("La proximidad del deadline no puede ser negativa."); }
    }
}
