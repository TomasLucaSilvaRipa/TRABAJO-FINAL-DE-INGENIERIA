using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLAnaliticaOperativa
{
    private readonly MPPAnaliticaOperativa _analiticaMPP;
    private readonly MPPRiesgoRetraso _riesgoMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly MPPAsignacionTarea _asignacionMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;
    private readonly MotorCargaOperativa _motorCargaOperativa;

    public BLLAnaliticaOperativa(MPPAnaliticaOperativa analiticaMPP, MPPRiesgoRetraso riesgoMPP, MPPRecursos recursosMPP, MPPAsignacionTarea asignacionMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL, EmailService emailService, MotorCargaOperativa motorCargaOperativa)
    {
        _analiticaMPP = analiticaMPP;
        _riesgoMPP = riesgoMPP;
        _recursosMPP = recursosMPP;
        _asignacionMPP = asignacionMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _emailService = emailService;
        _motorCargaOperativa = motorCargaOperativa;
    }

    public AnalisisOcupacionOperativa ConsultarOcupacion(Usuario usuario, FiltroOcupacionOperativa? filtro = null)
    {
        ValidarDueno(usuario);
        FiltroOcupacionOperativa filtroAplicado = filtro ?? new FiltroOcupacionOperativa();
        ValidarFiltroOcupacion(filtroAplicado);
        List<Empleado> empleados = _analiticaMPP.ConsultarEmpleadosActivos(usuario).Where(empleado => !filtroAplicado.IdEmpleado.HasValue || empleado.ID == filtroAplicado.IdEmpleado.Value).ToList();
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, filtroAplicado.IdProyecto, filtroAplicado.IdCliente);
        List<Tarea> tareas = contextos.SelectMany(contexto => contexto.Tareas).ToList();
        List<AsignacionTarea> asignaciones = _asignacionMPP.ConsultarActivas(tareas.Select(tarea => tarea.ID), usuario);
        List<RegistroHora> registros = contextos.SelectMany(contexto => contexto.Registros).ToList();
        (DateTime desde, DateTime hasta) = _motorCargaOperativa.ObtenerPeriodo(filtroAplicado);
        Dictionary<int, decimal> horasPlanificadasPorTarea = new Dictionary<int, decimal>();
        Dictionary<(int IdTarea, int IdEmpleado), decimal> horasPlanificadasPorTareaEmpleado = new Dictionary<(int IdTarea, int IdEmpleado), decimal>();
        AnalisisOcupacionOperativa resultado = new AnalisisOcupacionOperativa();
        foreach (Empleado empleado in empleados)
        {
            List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarAusenciasEmpleado(empleado, usuario);
            CargaOperativaEmpleado carga = _motorCargaOperativa.Calcular(empleado, tareas, asignaciones, registros, ausencias, desde, hasta);
            foreach ((int idTarea, decimal horas) in carga.HorasPlanificadasPorTarea)
            {
                horasPlanificadasPorTarea[idTarea] = horasPlanificadasPorTarea.GetValueOrDefault(idTarea) + horas;
                horasPlanificadasPorTareaEmpleado[(idTarea, empleado.ID)] = horas;
            }
            List<Tarea> tareasEmpleado = tareas.Where(tarea => carga.HorasPlanificadasPorTarea.ContainsKey(tarea.ID)).ToList();
            decimal horasPlanificadas = carga.HorasPlanificadasPeriodo;
            decimal porcentajeSemanal = CalcularPorcentaje(horasPlanificadas, carga.HorasCapacidadPeriodo);
            decimal porcentaje = Math.Max(porcentajeSemanal, carga.MayorPorcentajeOcupacionDiaria);
            string estado = ObtenerEstadoOcupacion(porcentaje);
            if (!string.IsNullOrWhiteSpace(filtroAplicado.EstadoCarga) && !string.Equals(estado, filtroAplicado.EstadoCarga, StringComparison.OrdinalIgnoreCase)) { continue; }
            Tarea? tareaPrincipal = tareasEmpleado.OrderByDescending(tarea => carga.HorasPlanificadasPorTarea[tarea.ID]).FirstOrDefault();
            OcupacionEmpleado detalle = new OcupacionEmpleado();
            detalle.IdEmpleado = empleado.ID;
            detalle.Nombre = empleado.Nombre;
            detalle.Seniority = empleado.Seniority;
            detalle.HorasDisponibles = carga.HorasCapacidadPeriodo;
            detalle.HorasEstimadasActivas = horasPlanificadas;
            detalle.HorasRegistradas = carga.HorasRegistradasPeriodo;
            detalle.PorcentajeOcupacion = porcentaje;
            detalle.EstadoCarga = estado;
            detalle.CausaPrincipal = tareaPrincipal is null ? "No tiene tareas planificables asignadas dentro del período seleccionado." : carga.TieneSobrecargaDiaria ? "La tarea '" + tareaPrincipal.Titulo + "' genera una sobrecarga en al menos un día laborable del período." : "La tarea '" + tareaPrincipal.Titulo + "' concentra la mayor carga planificada del período.";
            detalle.AccionSugerida = estado is "Sobrecargado" or "Crítico" ? "Revisar la distribución de tareas o evaluar un recurso alternativo." : estado == "Subutilizado" ? "Evaluar la asignación de trabajo compatible con su disponibilidad." : "Mantener seguimiento de la carga vigente.";
            detalle.Proyectos = contextos.Where(contexto => contexto.Tareas.Any(tarea => carga.HorasPlanificadasPorTarea.ContainsKey(tarea.ID))).Select(contexto => contexto.Proyecto.Nombre).Distinct().OrderBy(nombre => nombre).ToList();
            resultado.Empleados.Add(detalle);
        }
        resultado.Empleados = resultado.Empleados.OrderByDescending(empleado => empleado.PorcentajeOcupacion).ThenBy(empleado => empleado.Nombre).ToList();
        resultado.HorasDisponiblesSemanales = resultado.Empleados.Sum(empleado => empleado.HorasDisponibles);
        resultado.HorasEstimadasActivas = resultado.Empleados.Sum(empleado => empleado.HorasEstimadasActivas);
        resultado.PorcentajeOcupacionGeneral = CalcularPorcentaje(resultado.HorasEstimadasActivas, resultado.HorasDisponiblesSemanales);
        resultado.EstadoCargaGeneral = ObtenerEstadoOcupacion(resultado.PorcentajeOcupacionGeneral);
        resultado.CantidadSubutilizados = resultado.Empleados.Count(empleado => empleado.EstadoCarga == "Subutilizado");
        resultado.CantidadBalanceados = resultado.Empleados.Count(empleado => empleado.EstadoCarga == "Balanceado");
        resultado.CantidadSobrecargados = resultado.Empleados.Count(empleado => empleado.EstadoCarga is "Sobrecargado" or "Crítico");
        HashSet<int> empleadosIncluidos = resultado.Empleados.Select(empleado => empleado.IdEmpleado).ToHashSet();
        resultado.Proyectos = CrearDistribucionProyectos(contextos, resultado.HorasEstimadasActivas, empleadosIncluidos, horasPlanificadasPorTarea, horasPlanificadasPorTareaEmpleado);
        resultado.Clientes = CrearDistribucionClientes(resultado.Proyectos, resultado.HorasEstimadasActivas);
        return resultado;
    }

    /// <summary>
    /// Devuelve la carga del empleado autenticado durante la semana laboral vigente.
    /// No expone la distribución de carga del resto de la agencia.
    /// </summary>
    public OcupacionEmpleado ConsultarMiCargaOperativa(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "VerCargaOperativa")) { throw new UnauthorizedAccessException("No tenés permiso para consultar tu carga operativa."); }

        Empleado empleado = _analiticaMPP.ConsultarEmpleadosActivos(usuario)
            .FirstOrDefault(item => item.ID == usuario.ID)
            ?? throw new UnauthorizedAccessException("Tu cuenta no está asociada a un empleado activo de la agencia.");

        // Esta consulta se realiza sobre los proyectos de la agencia; el motor sólo incorpora
        // tareas que estén efectivamente asignadas al empleado autenticado.
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, null);
        List<Tarea> tareas = contextos.SelectMany(contexto => contexto.Tareas).ToList();
        List<AsignacionTarea> asignaciones = _asignacionMPP.ConsultarActivas(tareas.Select(tarea => tarea.ID), usuario);
        List<RegistroHora> registros = contextos.SelectMany(contexto => contexto.Registros).ToList();
        List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarAusenciasEmpleado(empleado, usuario);
        FiltroOcupacionOperativa filtro = new FiltroOcupacionOperativa();
        (DateTime desde, DateTime hasta) = _motorCargaOperativa.ObtenerPeriodo(filtro);
        CargaOperativaEmpleado carga = _motorCargaOperativa.Calcular(empleado, tareas, asignaciones, registros, ausencias, desde, hasta);
        decimal porcentajeSemanal = CalcularPorcentaje(carga.HorasPlanificadasPeriodo, carga.HorasCapacidadPeriodo);
        decimal porcentaje = Math.Max(porcentajeSemanal, carga.MayorPorcentajeOcupacionDiaria);
        List<Tarea> tareasEmpleado = tareas.Where(tarea => carga.HorasPlanificadasPorTarea.ContainsKey(tarea.ID)).ToList();
        Tarea? tareaPrincipal = tareasEmpleado.OrderByDescending(tarea => carga.HorasPlanificadasPorTarea[tarea.ID]).FirstOrDefault();
        string estado = ObtenerEstadoOcupacion(porcentaje);

        string causaPrincipal = tareaPrincipal is null
                ? "No tenés tareas planificables asignadas durante esta semana."
                : carga.TieneSobrecargaDiaria
                    ? "La tarea '" + tareaPrincipal.Titulo + "' concentra una sobrecarga en al menos un día hábil."
                    : "La tarea '" + tareaPrincipal.Titulo + "' concentra la mayor parte de tu carga semanal.";
        string accionSugerida = estado is "Sobrecargado" or "Crítico"
                ? "Revisá tus prioridades con tu responsable si necesitás redistribuir trabajo."
                : "Tu carga se actualiza al cambiar tus tareas, disponibilidad o ausencias.";
        List<string> proyectos = contextos
                .Where(contexto => contexto.Tareas.Any(tarea => carga.HorasPlanificadasPorTarea.ContainsKey(tarea.ID)))
                .Select(contexto => contexto.Proyecto.Nombre)
                .Distinct()
                .OrderBy(nombre => nombre)
                .ToList();
        return new OcupacionEmpleado(empleado.ID, empleado.Nombre, empleado.Seniority, carga.HorasCapacidadPeriodo, carga.HorasPlanificadasPeriodo, carga.HorasRegistradasPeriodo, porcentaje, estado, causaPrincipal, accionSugerida, proyectos);
    }

    public DetalleOcupacionEmpleado ConsultarDetalleOcupacionEmpleado(Usuario usuario, FiltroOcupacionOperativa filtro)
    {
        ValidarDueno(usuario);
        if (!filtro.IdEmpleado.HasValue) { throw new ArgumentException("Seleccioná un empleado para consultar el detalle de ocupación."); }
        AnalisisOcupacionOperativa analisis = ConsultarOcupacion(usuario, filtro);
        OcupacionEmpleado empleado = analisis.Empleados.FirstOrDefault(item => item.IdEmpleado == filtro.IdEmpleado.Value) ?? throw new KeyNotFoundException("No se encontró el empleado dentro de los filtros seleccionados.");
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, filtro.IdProyecto, filtro.IdCliente);
        List<Tarea> tareas = contextos.SelectMany(contexto => contexto.Tareas).ToList();
        List<AsignacionTarea> asignaciones = _asignacionMPP.ConsultarActivas(tareas.Select(tarea => tarea.ID), usuario);
        List<RegistroHora> registros = contextos.SelectMany(contexto => contexto.Registros).ToList();
        Empleado empleadoBase = _analiticaMPP.ConsultarEmpleadosActivos(usuario).First(item => item.ID == empleado.IdEmpleado);
        (DateTime desde, DateTime hasta) = _motorCargaOperativa.ObtenerPeriodo(filtro);
        List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarAusenciasEmpleado(empleadoBase, usuario);
        CargaOperativaEmpleado carga = _motorCargaOperativa.Calcular(empleadoBase, tareas, asignaciones, registros, ausencias, desde, hasta);
        DetalleOcupacionEmpleado detalle = new DetalleOcupacionEmpleado();
        detalle.Empleado = empleado;
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            foreach (Tarea tarea in contexto.Tareas.Where(item => carga.HorasPlanificadasPorTarea.ContainsKey(item.ID)))
            {
                DetalleOcupacionTarea tareaDetalle = new DetalleOcupacionTarea();
                tareaDetalle.IdTarea = tarea.ID;
                tareaDetalle.IdProyecto = contexto.Proyecto.ID;
                tareaDetalle.IdPMResponsable = contexto.Proyecto.IdPMResponsable;
                tareaDetalle.Titulo = tarea.Titulo;
                tareaDetalle.NombreProyecto = contexto.Proyecto.Nombre;
                tareaDetalle.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
                tareaDetalle.HorasEstimadas = carga.HorasPlanificadasPorTarea[tarea.ID];
                tareaDetalle.IncidenciaSobreCargaEmpleado = CalcularPorcentaje(tareaDetalle.HorasEstimadas, empleado.HorasEstimadasActivas);
                detalle.Tareas.Add(tareaDetalle);
            }
        }
        detalle.Tareas = detalle.Tareas.OrderByDescending(tarea => tarea.HorasEstimadas).ToList();
        return detalle;
    }

    public async Task SolicitarRevisionOperativa(Usuario usuario, SolicitudRevisionOperativaNueva nuevaSolicitud)
    {
        ValidarDueno(usuario);
        if (nuevaSolicitud.IdEmpleado <= 0) { throw new ArgumentException("Seleccioná un empleado para solicitar la revisión."); }
        FiltroOcupacionOperativa filtro = new FiltroOcupacionOperativa();
        filtro.IdEmpleado = nuevaSolicitud.IdEmpleado;
        filtro.IdProyecto = nuevaSolicitud.IdProyecto;
        DetalleOcupacionEmpleado detalle = ConsultarDetalleOcupacionEmpleado(usuario, filtro);
        DetalleOcupacionTarea? tarea = nuevaSolicitud.IdTarea.HasValue ? detalle.Tareas.FirstOrDefault(item => item.IdTarea == nuevaSolicitud.IdTarea.Value) : detalle.Tareas.FirstOrDefault();
        if (tarea is null) { throw new InvalidOperationException("El empleado no tiene una tarea activa asociada para derivar al PM responsable."); }
        SolicitudRevisionOperativa solicitud = new SolicitudRevisionOperativa();
        solicitud.IdAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
        solicitud.IdEmpleado = detalle.Empleado.IdEmpleado;
        solicitud.IdProyecto = tarea.IdProyecto;
        solicitud.IdTarea = tarea.IdTarea;
        solicitud.IdPMResponsable = tarea.IdPMResponsable;
        solicitud.IdUsuarioSolicitante = usuario.ID;
        solicitud.CausaPrincipal = detalle.Empleado.CausaPrincipal;
        solicitud.AccionSugerida = detalle.Empleado.AccionSugerida;
        solicitud.ID = _analiticaMPP.RegistrarSolicitudRevision(solicitud);
        DestinatarioDesvioHoras? destinatario = _analiticaMPP.ConsultarDestinatarioPM(solicitud.IdPMResponsable);
        bool correoEnviado = destinatario is not null && await _emailService.EnviarCorreoSolicitudRevisionOperativa(destinatario.Email, destinatario.Nombre, detalle.Empleado.Nombre, tarea.NombreProyecto, solicitud.CausaPrincipal, solicitud.AccionSugerida);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SolicitudRevisionOperativa", solicitud.ID, "SolicitarRevisionOperativa", "Se derivó una revisión operativa del empleado '" + detalle.Empleado.Nombre + "' al PM responsable.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia", "Proyectos"));
    }

    public List<RevisionOperativaPendiente> ConsultarSolicitudesRevisionPendientes(Usuario usuario)
    {
        ValidarPMODueno(usuario);
        if (!EsPMSinRolDueno(usuario)) { throw new UnauthorizedAccessException("Las revisiones pendientes se consultan desde la cuenta del PM responsable."); }
        int idPM = _analiticaMPP.ConsultarIdPM(usuario);
        List<RevisionOperativaPendiente> solicitudes = _analiticaMPP.ConsultarSolicitudesPendientes(usuario, idPM)
            .Select(solicitud => new RevisionOperativaPendiente(solicitud.ID, "Asignacion", null, string.Empty, solicitud.NombreEmpleado, solicitud.NombreProyecto, solicitud.CausaPrincipal, solicitud.AccionSugerida, solicitud.Estado, solicitud.FechaAlta)).ToList();
        solicitudes.AddRange(_analiticaMPP.ConsultarSolicitudesRevisionConvenienciaPendientes(usuario, idPM)
            .Select(solicitud => new RevisionOperativaPendiente(solicitud.ID, "ConvenienciaCliente", solicitud.IdCliente, solicitud.NombreCliente, "Conveniencia del cliente", "Todos los proyectos a cargo", solicitud.CausaPrincipal, solicitud.AccionSugerida, solicitud.Estado, solicitud.FechaAlta)));
        solicitudes.AddRange(_analiticaMPP.ConsultarSolicitudesRevisionGestionPendientes(usuario, idPM)
            .Select(solicitud => new RevisionOperativaPendiente(solicitud.ID, solicitud.Tipo, null, string.Empty, solicitud.TituloTarea ?? solicitud.NombreProyecto, solicitud.NombreProyecto, solicitud.CausaPrincipal, solicitud.AccionSugerida, solicitud.Estado, solicitud.FechaAlta)));
        return solicitudes.OrderByDescending(solicitud => solicitud.FechaAlta).ToList();
    }

    public void MarcarSolicitudRevisionResuelta(Usuario usuario, int idSolicitud, string? tipo)
    {
        ValidarPMODueno(usuario);
        if (!EsPMSinRolDueno(usuario)) { throw new UnauthorizedAccessException("Sólo el PM responsable puede resolver sus solicitudes de revisión."); }
        if (idSolicitud <= 0) { throw new ArgumentException("La solicitud de revisión seleccionada no es válida."); }
        int idPM = _analiticaMPP.ConsultarIdPM(usuario);
        bool esRevisionConveniencia = string.Equals(tipo, "ConvenienciaCliente", StringComparison.OrdinalIgnoreCase);
        bool esRevisionGestion = string.Equals(tipo, "ControlHoras", StringComparison.OrdinalIgnoreCase) || string.Equals(tipo, "RiesgoRetraso", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(tipo) && !esRevisionConveniencia && !esRevisionGestion && !string.Equals(tipo, "Asignacion", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("El tipo de revisión seleccionado no es válido."); }
        bool actualizada = esRevisionConveniencia
            ? _analiticaMPP.MarcarSolicitudRevisionConvenienciaResuelta(usuario, idPM, idSolicitud)
            : esRevisionGestion
                ? _analiticaMPP.MarcarSolicitudRevisionGestionResuelta(usuario, idPM, idSolicitud)
                : _analiticaMPP.MarcarSolicitudRevisionResuelta(usuario, idPM, idSolicitud);
        if (!actualizada)
        {
            throw new KeyNotFoundException("La solicitud no existe, no te corresponde o ya fue resuelta.");
        }
        string entidad = esRevisionConveniencia ? "SolicitudRevisionConveniencia" : esRevisionGestion ? "SolicitudRevisionGestion" : "SolicitudRevisionOperativa";
        string descripcion = esRevisionConveniencia ? "El PM responsable resolvió una revisión de conveniencia de cliente." : esRevisionGestion ? "El PM responsable resolvió una revisión de gestión operativa." : "El PM responsable marcó como resuelta una solicitud de revisión operativa.";
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, entidad, idSolicitud, "MarcarSolicitudRevisionResuelta", descripcion, "Exitoso", "Informacion", "Proyectos"));
    }

    public AnalisisDesvioHoras ConsultarDesvioHoras(Usuario usuario, FiltroDesvioHoras? filtro = null)
    {
        ValidarPMODueno(usuario);
        FiltroDesvioHoras filtroAplicado = filtro ?? new FiltroDesvioHoras();
        ValidarFiltroDesvio(filtroAplicado);
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, filtroAplicado.IdProyecto);
        AnalisisDesvioHoras resultado = new AnalisisDesvioHoras();
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            DesvioHorasProyecto proyecto = new DesvioHorasProyecto();
            proyecto.IdProyecto = contexto.Proyecto.ID;
            proyecto.NombreProyecto = contexto.Proyecto.Nombre;
            proyecto.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
            List<Tarea> tareasFiltradas = contexto.Tareas.Where(tarea => CumpleFiltroTarea(tarea, filtroAplicado)).ToList();
            foreach (Tarea tarea in tareasFiltradas)
            {
                decimal horasReales = contexto.Registros.Where(registro => registro.IdTarea == tarea.ID && registro.Activo && CumplePeriodo(registro, filtroAplicado)).Sum(registro => registro.CantidadHoras);
                DesvioHorasTarea detalle = new DesvioHorasTarea();
                detalle.IdTarea = tarea.ID;
                detalle.IdEmpleadoResponsable = tarea.IdEmpleadoAsignado;
                detalle.Titulo = tarea.Titulo;
                detalle.Responsable = ObtenerNombreEmpleado(contexto, tarea.IdEmpleadoAsignado);
                detalle.EstadoTarea = tarea.Estado ?? string.Empty;
                detalle.HorasEstimadas = tarea.HorasEstimadas;
                detalle.HorasReales = horasReales;
                detalle.DesvioHoras = horasReales - tarea.HorasEstimadas;
                CalcularBalanceHoras(detalle.HorasEstimadas, detalle.HorasReales, out decimal horasRestantes, out decimal horasExcedidas);
                detalle.HorasRestantes = horasRestantes;
                detalle.HorasExcedidas = horasExcedidas;
                detalle.PorcentajeConsumo = CalcularPorcentaje(horasReales, tarea.HorasEstimadas);
                detalle.Estado = ObtenerEstadoDesvio(detalle.PorcentajeConsumo);
                if (CumpleNivelDesvio(detalle.Estado, filtroAplicado.NivelDesvio)) { proyecto.Tareas.Add(detalle); }
            }
            if (proyecto.Tareas.Count == 0) { continue; }
            proyecto.Tareas = proyecto.Tareas.OrderByDescending(tarea => tarea.PorcentajeConsumo).ThenByDescending(tarea => tarea.DesvioHoras).ToList();
            proyecto.HorasEstimadas = proyecto.Tareas.Sum(tarea => tarea.HorasEstimadas);
            proyecto.HorasReales = proyecto.Tareas.Sum(tarea => tarea.HorasReales);
            proyecto.DesvioHoras = proyecto.HorasReales - proyecto.HorasEstimadas;
            CalcularBalanceHoras(proyecto.HorasEstimadas, proyecto.HorasReales, out decimal horasRestantesProyecto, out decimal horasExcedidasProyecto);
            proyecto.HorasRestantes = horasRestantesProyecto;
            proyecto.HorasExcedidas = horasExcedidasProyecto;
            proyecto.PorcentajeConsumo = CalcularPorcentaje(proyecto.HorasReales, proyecto.HorasEstimadas);
            proyecto.Estado = ObtenerEstadoDesvio(proyecto.PorcentajeConsumo);
            resultado.Proyectos.Add(proyecto);
        }
        resultado.Proyectos = resultado.Proyectos.OrderByDescending(proyecto => proyecto.PorcentajeConsumo).ThenByDescending(proyecto => proyecto.DesvioHoras).ToList();
        resultado.HorasEstimadas = resultado.Proyectos.Sum(proyecto => proyecto.HorasEstimadas);
        resultado.HorasReales = resultado.Proyectos.Sum(proyecto => proyecto.HorasReales);
        resultado.DesvioHoras = resultado.HorasReales - resultado.HorasEstimadas;
        CalcularBalanceHoras(resultado.HorasEstimadas, resultado.HorasReales, out decimal horasRestantesResultado, out decimal horasExcedidasResultado);
        resultado.HorasRestantes = horasRestantesResultado;
        resultado.HorasExcedidas = horasExcedidasResultado;
        resultado.PorcentajeConsumo = CalcularPorcentaje(resultado.HorasReales, resultado.HorasEstimadas);
        return resultado;
    }

    public DetalleDesvioHorasTarea ConsultarDetalleDesvioHorasTarea(Usuario usuario, int idProyecto, int idTarea, DateTime? desde, DateTime? hasta)
    {
        ValidarPMODueno(usuario);
        FiltroDesvioHoras filtro = new FiltroDesvioHoras();
        filtro.IdProyecto = idProyecto;
        filtro.Desde = desde;
        filtro.Hasta = hasta;
        ValidarFiltroDesvio(filtro);
        ContextoProyectoAnalitico contexto = ConsultarContextos(usuario, idProyecto).FirstOrDefault() ?? throw new KeyNotFoundException("No existe el proyecto solicitado o no tenés acceso a él.");
        Tarea tarea = contexto.Tareas.FirstOrDefault(item => item.ID == idTarea && item.Activo) ?? throw new KeyNotFoundException("La tarea seleccionada no pertenece al proyecto.");
        List<RegistroHora> registros = contexto.Registros.Where(registro => registro.IdTarea == tarea.ID && registro.Activo && CumplePeriodo(registro, filtro)).OrderByDescending(registro => registro.Fecha).ToList();
        DetalleDesvioHorasTarea detalle = new DetalleDesvioHorasTarea();
        detalle.IdProyecto = contexto.Proyecto.ID;
        detalle.IdTarea = tarea.ID;
        detalle.NombreProyecto = contexto.Proyecto.Nombre;
        detalle.Titulo = tarea.Titulo;
        detalle.Responsable = ObtenerNombreEmpleado(contexto, tarea.IdEmpleadoAsignado);
        detalle.EstadoTarea = tarea.Estado ?? string.Empty;
        detalle.HorasEstimadas = tarea.HorasEstimadas;
        detalle.HorasReales = registros.Sum(registro => registro.CantidadHoras);
        detalle.DesvioHoras = detalle.HorasReales - detalle.HorasEstimadas;
        CalcularBalanceHoras(detalle.HorasEstimadas, detalle.HorasReales, out decimal horasRestantes, out decimal horasExcedidas);
        detalle.HorasRestantes = horasRestantes;
        detalle.HorasExcedidas = horasExcedidas;
        detalle.PorcentajeConsumo = CalcularPorcentaje(detalle.HorasReales, detalle.HorasEstimadas);
        detalle.EstadoDesvio = ObtenerEstadoDesvio(detalle.PorcentajeConsumo);
        detalle.RegistrosHoras = registros;
        return detalle;
    }

    public async Task<string> RegistrarAccionAjusteDesvioHoras(Usuario usuario, AccionAjusteDesvioHoras accion)
    {
        ValidarPMODueno(usuario);
        if (accion.IdProyecto <= 0) { throw new ArgumentException("Seleccioná un proyecto para registrar la acción de ajuste."); }
        if (string.IsNullOrWhiteSpace(accion.Observacion)) { throw new ArgumentException("Describí la acción de ajuste antes de confirmarla."); }
        if (accion.Observacion.Trim().Length > 1500) { throw new ArgumentException("La observación no puede superar los 1500 caracteres."); }
        ContextoProyectoAnalitico contexto = ConsultarContextos(usuario, accion.IdProyecto).FirstOrDefault() ?? throw new KeyNotFoundException("No existe el proyecto solicitado o no tenés acceso a él.");
        Tarea? tarea = null;
        if (accion.IdTarea.HasValue)
        {
            ConsultarDetalleDesvioHorasTarea(usuario, accion.IdProyecto, accion.IdTarea.Value, null, null);
            tarea = contexto.Tareas.FirstOrDefault(item => item.ID == accion.IdTarea.Value);
        }
        SolicitudRevisionGestion solicitud = new SolicitudRevisionGestion
        {
            IdAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."), IdProyecto = contexto.Proyecto.ID,
            IdTarea = accion.IdTarea, IdPMResponsable = contexto.Proyecto.IdPMResponsable, IdUsuarioSolicitante = usuario.ID,
            Tipo = "ControlHoras", CausaPrincipal = "Se registró un ajuste para el control de horas.", AccionSugerida = accion.Observacion.Trim()
        };
        solicitud.ID = _analiticaMPP.RegistrarSolicitudRevisionGestion(solicitud);
        DestinatarioDesvioHoras? destinatario = _analiticaMPP.ConsultarDestinatarioPM(solicitud.IdPMResponsable);
        bool correoEnviado = destinatario is not null && !string.IsNullOrWhiteSpace(destinatario.Email) && await _emailService.EnviarCorreoSolicitudRevisionGestion(destinatario.Email, destinatario.Nombre, "control de horas", contexto.Proyecto.Nombre, tarea?.Titulo, solicitud.CausaPrincipal, solicitud.AccionSugerida);
        string entidad = accion.IdTarea.HasValue ? "Tarea" : "Proyecto";
        int idEntidad = accion.IdTarea ?? accion.IdProyecto;
        string resultado = correoEnviado ? "Exitoso" : "Parcial";
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, entidad, idEntidad, "RegistrarAccionAjusteDesvioHoras", "Acción de ajuste registrada y derivada al PM: " + accion.Observacion.Trim(), resultado, correoEnviado ? "Informacion" : "Advertencia", "Proyectos"));
        return correoEnviado ? "El ajuste quedó registrado, enviado al PM y disponible en sus revisiones pendientes." : "El ajuste quedó registrado y disponible en las revisiones del PM, pero no se pudo enviar el correo.";
    }

    public async Task ComunicarDesvioHorasADueno(Usuario usuario, ComunicacionDesvioHoras comunicacion)
    {
        ValidarPMODueno(usuario);
        if (comunicacion.IdProyecto <= 0) { throw new ArgumentException("Seleccioná el proyecto cuyo desvío querés informar."); }
        FiltroDesvioHoras filtro = comunicacion.Filtro ?? new FiltroDesvioHoras();
        filtro.IdProyecto = comunicacion.IdProyecto;
        AnalisisDesvioHoras analisis = ConsultarDesvioHoras(usuario, filtro);
        DesvioHorasProyecto proyecto = analisis.Proyectos.FirstOrDefault(item => item.IdProyecto == comunicacion.IdProyecto) ?? throw new InvalidOperationException("No hay información de desvío para comunicar con los filtros seleccionados.");
        List<DestinatarioDesvioHoras> destinatarios = _analiticaMPP.ConsultarDuenosAgencia(usuario);
        if (destinatarios.Count == 0) { throw new InvalidOperationException("No se encontró un Dueño activo para recibir el desvío."); }
        List<string> tareas = proyecto.Tareas.OrderByDescending(tarea => tarea.DesvioHoras).ThenByDescending(tarea => tarea.PorcentajeConsumo).Take(5).Select(tarea => tarea.Titulo + ": " + tarea.DesvioHoras.ToString("N2") + " h de desvío (" + tarea.PorcentajeConsumo.ToString("N2") + "% de consumo)").ToList();
        int enviados = 0;
        foreach (DestinatarioDesvioHoras destinatario in destinatarios)
        {
            if (await _emailService.EnviarCorreoDesvioHoras(destinatario.Email, destinatario.Nombre, proyecto.NombreProyecto, proyecto.HorasEstimadas, proyecto.HorasReales, proyecto.DesvioHoras, proyecto.PorcentajeConsumo, tareas)) { enviados++; }
        }
        string resultado = enviados == destinatarios.Count ? "Exitoso" : enviados > 0 ? "Parcial" : "Fallido";
        string criticidad = enviados == destinatarios.Count ? "Informacion" : "Advertencia";
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Proyecto", proyecto.IdProyecto, "InformarDesvioHorasADueno", "Se informó el desvío de horas del proyecto '" + proyecto.NombreProyecto + "' a " + enviados + " de " + destinatarios.Count + " Dueño(s).", resultado, criticidad, "Proyectos"));
        if (enviados == 0) { throw new InvalidOperationException("No fue posible enviar el correo al Dueño. Revisá la configuración de correo de TeamBalance."); }
    }

    public AnalisisConvenienciaOperativa ConsultarConveniencia(Usuario usuario, FiltroConvenienciaOperativa? filtro = null)
    {
        ValidarDueno(usuario);
        FiltroConvenienciaOperativa filtroAplicado = filtro ?? new FiltroConvenienciaOperativa();
        ValidarFiltroConveniencia(filtroAplicado);
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, filtroAplicado.IdProyecto, filtroAplicado.IdCliente)
            .Where(contexto => string.IsNullOrWhiteSpace(filtroAplicado.EstadoProyecto) || string.Equals(contexto.Proyecto.Estado, filtroAplicado.EstadoProyecto, StringComparison.OrdinalIgnoreCase))
            .ToList();
        List<AsignacionTarea> asignaciones = ConsultarAsignacionesActivas(contextos, usuario);
        Dictionary<int, Empleado> empleados = _analiticaMPP.ConsultarEmpleadosActivos(usuario).ToDictionary(empleado => empleado.ID);
        AnalisisConvenienciaOperativa resultado = new AnalisisConvenienciaOperativa();

        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            List<Tarea> tareas = contexto.Tareas.Where(tarea => CumplePeriodoConveniencia(tarea, filtroAplicado)).ToList();
            if (tareas.Count == 0) { continue; }
            List<RegistroHora> registros = contexto.Registros.Where(registro => tareas.Any(tarea => tarea.ID == registro.IdTarea) && CumplePeriodo(registro, filtroAplicado.Desde, filtroAplicado.Hasta)).ToList();
            ConvenienciaOperativaProyecto detalle = ConstruirConvenienciaProyecto(contexto, tareas, registros, asignaciones, empleados, out _);
            if (CumpleNivelConveniencia(detalle, filtroAplicado.NivelDesvioCosto)) { resultado.Proyectos.Add(detalle); }
        }

        resultado.Proyectos = resultado.Proyectos.OrderByDescending(proyecto => proyecto.Sobrecosto).ThenByDescending(proyecto => proyecto.HorasRealesSinCosto).ThenBy(proyecto => proyecto.NombreCliente).ToList();
        HashSet<int> clientesEnRevision = _analiticaMPP.ConsultarClientesEnRevision(usuario).ToHashSet();
        resultado.Clientes = CrearResumenClientes(resultado.Proyectos, clientesEnRevision);
        resultado.CostoPlanificado = resultado.Proyectos.Sum(proyecto => proyecto.CostoPlanificado);
        resultado.CostoReal = resultado.Proyectos.Sum(proyecto => proyecto.CostoReal);
        resultado.DesvioCosto = resultado.CostoReal - resultado.CostoPlanificado;
        resultado.PresupuestoNoConsumido = Math.Max(resultado.CostoPlanificado - resultado.CostoReal, 0);
        resultado.Sobrecosto = Math.Max(resultado.CostoReal - resultado.CostoPlanificado, 0);
        resultado.HorasRealesSinCosto = resultado.Proyectos.Sum(proyecto => proyecto.HorasRealesSinCosto);
        resultado.CantidadClientesEnRevision = resultado.Clientes.Count(cliente => cliente.EnRevisionOperativa);
        return resultado;
    }

    public DetalleConvenienciaOperativa ConsultarDetalleConveniencia(Usuario usuario, int idProyecto, FiltroConvenienciaOperativa? filtro = null)
    {
        ValidarDueno(usuario);
        if (idProyecto <= 0) { throw new ArgumentException("Seleccioná un proyecto para consultar su composición de costo."); }
        FiltroConvenienciaOperativa filtroAplicado = filtro ?? new FiltroConvenienciaOperativa();
        filtroAplicado.IdProyecto = idProyecto;
        ValidarFiltroConveniencia(filtroAplicado);
        ContextoProyectoAnalitico contexto = ConsultarContextos(usuario, idProyecto, filtroAplicado.IdCliente).FirstOrDefault()
            ?? throw new KeyNotFoundException("No existe el proyecto solicitado o no tenés acceso a él.");
        if (!string.IsNullOrWhiteSpace(filtroAplicado.EstadoProyecto) && !string.Equals(contexto.Proyecto.Estado, filtroAplicado.EstadoProyecto, StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException("El proyecto no cumple con el estado seleccionado.");
        }
        List<Tarea> tareas = contexto.Tareas.Where(tarea => CumplePeriodoConveniencia(tarea, filtroAplicado)).ToList();
        List<RegistroHora> registros = contexto.Registros.Where(registro => tareas.Any(tarea => tarea.ID == registro.IdTarea) && CumplePeriodo(registro, filtroAplicado.Desde, filtroAplicado.Hasta)).ToList();
        List<AsignacionTarea> asignaciones = ConsultarAsignacionesActivas(new[] { contexto }, usuario);
        Dictionary<int, Empleado> empleados = _analiticaMPP.ConsultarEmpleadosActivos(usuario).ToDictionary(empleado => empleado.ID);
        ConvenienciaOperativaProyecto proyecto = ConstruirConvenienciaProyecto(contexto, tareas, registros, asignaciones, empleados, out List<ComposicionCostoOperativo> composicion);
        return new DetalleConvenienciaOperativa
        {
            Proyecto = proyecto,
            ComposicionCosto = composicion.OrderByDescending(item => item.CostoReal).ThenByDescending(item => item.CostoPlanificado).ToList(),
            Seguimientos = _analiticaMPP.ConsultarSeguimientosConveniencia(usuario, contexto.Proyecto.IdCliente, contexto.Proyecto.ID)
        };
    }

    public void RegistrarSeguimientoConveniencia(Usuario usuario, NuevoSeguimientoConvenienciaOperativa seguimiento)
    {
        ValidarDueno(usuario);
        if (seguimiento.IdCliente <= 0) { throw new ArgumentException("Seleccioná un cliente para registrar el seguimiento."); }
        if (string.IsNullOrWhiteSpace(seguimiento.Observacion) || string.IsNullOrWhiteSpace(seguimiento.AccionSugerida)) { throw new ArgumentException("Describí la observación y la acción sugerida."); }
        if (seguimiento.Observacion.Trim().Length > 1500 || seguimiento.AccionSugerida.Trim().Length > 1500) { throw new ArgumentException("La observación y la acción sugerida no pueden superar los 1500 caracteres."); }
        if (!ConsultarContextos(usuario, seguimiento.IdProyecto, seguimiento.IdCliente).Any()) { throw new KeyNotFoundException("El cliente o proyecto seleccionado no pertenece a tu agencia."); }
        seguimiento.Observacion = seguimiento.Observacion.Trim();
        seguimiento.AccionSugerida = seguimiento.AccionSugerida.Trim();
        int idSeguimiento = _analiticaMPP.RegistrarSeguimientoConveniencia(usuario, seguimiento);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SeguimientoConvenienciaOperativa", idSeguimiento, "RegistrarSeguimientoConveniencia", "Se registró un seguimiento de conveniencia operativa.", "Exitoso", "Informacion", "Proyectos"));
    }

    public async Task<string> MarcarClienteRevisionConveniencia(Usuario usuario, int idCliente)
    {
        ValidarDueno(usuario);
        if (idCliente <= 0) { throw new ArgumentException("Seleccioná un cliente para marcarlo en revisión."); }
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, null, idCliente);
        if (!contextos.Any()) { throw new KeyNotFoundException("El cliente seleccionado no pertenece a tu agencia."); }
        _analiticaMPP.MarcarClienteRevisionConveniencia(usuario, idCliente);
        string nombreCliente = contextos[0].Proyecto.NombreCliente ?? string.Empty;
        string causa = "El cliente '" + nombreCliente + "' fue marcado para una revisión de conveniencia operativa.";
        string accion = "Revisar los costos planificados, reales y la composición de los proyectos a cargo.";
        int creadas = 0;
        int correosEnviados = 0;
        foreach (IGrouping<int, ContextoProyectoAnalitico> grupoPM in contextos.GroupBy(contexto => contexto.Proyecto.IdPMResponsable))
        {
            SolicitudRevisionConveniencia solicitud = new SolicitudRevisionConveniencia
            {
                IdAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."),
                IdCliente = idCliente, IdPMResponsable = grupoPM.Key, IdUsuarioSolicitante = usuario.ID,
                CausaPrincipal = causa, AccionSugerida = accion
            };
            (int idSolicitud, bool creada) = _analiticaMPP.RegistrarSolicitudRevisionConveniencia(solicitud);
            if (!creada) { continue; }
            creadas++;
            DestinatarioDesvioHoras? destinatario = _analiticaMPP.ConsultarDestinatarioPM(grupoPM.Key);
            if (destinatario is not null && await _emailService.EnviarCorreoRevisionConveniencia(destinatario.Email, destinatario.Nombre, nombreCliente, grupoPM.Select(contexto => contexto.Proyecto.Nombre).Distinct().ToList(), causa, accion)) { correosEnviados++; }
        }
        string resultado = creadas == 0 ? "Sin cambios" : correosEnviados == creadas ? "Exitoso" : "Parcial";
        string detalle = creadas == 0
            ? "El cliente ya tenía una revisión de conveniencia pendiente para los PM responsables."
            : "Se derivaron " + creadas + " revisión(es) de conveniencia a los PM responsables; se enviaron " + correosEnviados + " correo(s).";
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Cliente", idCliente, "MarcarClienteRevisionConveniencia", detalle, resultado, resultado == "Exitoso" ? "Informacion" : "Advertencia", "Proyectos"));
        return detalle;
    }

    public AnalisisSobrecalificacionPerfiles ConsultarSobrecalificacion(Usuario usuario, int? idProyecto)
    {
        ValidarDueno(usuario);
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, idProyecto);
        List<AsignacionTarea> asignaciones = ConsultarAsignacionesActivas(contextos, usuario);
        AnalisisSobrecalificacionPerfiles resultado = new AnalisisSobrecalificacionPerfiles();
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            foreach (Tarea tarea in contexto.Tareas)
            {
                foreach (AsignacionTarea asignacion in ObtenerDistribucionTarea(tarea, asignaciones))
                {
                    Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == asignacion.IdEmpleado);
                    string seniorityRequerido = ObtenerSeniorityRequerido(tarea);
                    if (empleado is null || ObtenerNivelSeniority(empleado.Seniority) <= ObtenerNivelSeniority(seniorityRequerido)) { continue; }
                    decimal horasRegistradas = contexto.Registros.Where(registro => registro.IdTarea == tarea.ID && registro.IdEmpleado == empleado.ID).Sum(registro => registro.CantidadHoras);
                    decimal horasAfectadas = horasRegistradas > 0 ? horasRegistradas : asignacion.HorasAsignadas;
                    SobrecalificacionTarea detalle = new SobrecalificacionTarea();
                    detalle.IdTarea = tarea.ID;
                    detalle.IdProyecto = contexto.Proyecto.ID;
                    detalle.IdEmpleado = empleado.ID;
                    detalle.Titulo = tarea.Titulo;
                    detalle.NombreProyecto = contexto.Proyecto.Nombre;
                    detalle.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
                    detalle.SeniorityRequerido = seniorityRequerido;
                    detalle.SeniorityAsignado = empleado.Seniority;
                    detalle.EmpleadoAsignado = empleado.Nombre;
                    detalle.HorasAfectadas = horasAfectadas;
                    detalle.ImpactoOperativoEstimado = horasAfectadas * empleado.CostoHora;
                    detalle.Nivel = ObtenerNivelSeniority(empleado.Seniority) - ObtenerNivelSeniority(seniorityRequerido) > 1 ? "Alto" : "Moderado";
                    resultado.Tareas.Add(detalle);
                }
            }
        }
        resultado.Tareas = resultado.Tareas.OrderBy(tarea => tarea.EmpleadoAsignado).ThenByDescending(tarea => tarea.ImpactoOperativoEstimado).ThenByDescending(tarea => tarea.HorasAfectadas).ToList();
        resultado.HorasAfectadas = resultado.Tareas.Sum(tarea => tarea.HorasAfectadas);
        resultado.ImpactoOperativoEstimado = resultado.Tareas.Sum(tarea => tarea.ImpactoOperativoEstimado);
        resultado.CantidadTareasAfectadas = resultado.Tareas.Select(tarea => tarea.IdTarea).Distinct().Count();
        return resultado;
    }

    public async Task SolicitarRevisionSobrecalificacion(Usuario usuario, SolicitudRevisionSobrecalificacionNueva nuevaSolicitud)
    {
        ValidarDueno(usuario);
        if (nuevaSolicitud.IdEmpleado <= 0 || nuevaSolicitud.IdProyecto <= 0 || nuevaSolicitud.IdTarea <= 0)
        {
            throw new ArgumentException("Seleccioná una asignación sobrecalificada válida para revisar.");
        }
        if (string.IsNullOrWhiteSpace(nuevaSolicitud.Observacion))
        {
            throw new ArgumentException("Describí el criterio de asignación que debe revisar el PM.");
        }
        if (nuevaSolicitud.Observacion.Trim().Length > 1500)
        {
            throw new ArgumentException("La observación no puede superar los 1500 caracteres.");
        }

        SobrecalificacionTarea detalle = ConsultarSobrecalificacion(usuario, nuevaSolicitud.IdProyecto).Tareas.FirstOrDefault(item => item.IdProyecto == nuevaSolicitud.IdProyecto && item.IdTarea == nuevaSolicitud.IdTarea && item.IdEmpleado == nuevaSolicitud.IdEmpleado)
            ?? throw new KeyNotFoundException("La asignación ya no forma parte del análisis de sobrecalificación actual.");
        ContextoProyectoAnalitico contexto = ConsultarContextos(usuario, nuevaSolicitud.IdProyecto).FirstOrDefault(item => item.Proyecto.ID == nuevaSolicitud.IdProyecto)
            ?? throw new KeyNotFoundException("No existe el proyecto solicitado o no tenés acceso a él.");

        SolicitudRevisionOperativa solicitud = new SolicitudRevisionOperativa
        {
            IdAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."),
            IdEmpleado = detalle.IdEmpleado,
            IdProyecto = detalle.IdProyecto,
            IdTarea = detalle.IdTarea,
            IdPMResponsable = contexto.Proyecto.IdPMResponsable,
            IdUsuarioSolicitante = usuario.ID,
            CausaPrincipal = $"La tarea '{detalle.Titulo}' requiere seniority {detalle.SeniorityRequerido} y está asignada a un perfil {detalle.SeniorityAsignado}.",
            AccionSugerida = nuevaSolicitud.Observacion.Trim()
        };
        solicitud.ID = _analiticaMPP.RegistrarSolicitudRevision(solicitud);
        DestinatarioDesvioHoras? destinatario = _analiticaMPP.ConsultarDestinatarioPM(solicitud.IdPMResponsable);
        bool correoEnviado = destinatario is not null && await _emailService.EnviarCorreoSolicitudRevisionOperativa(destinatario.Email, destinatario.Nombre, detalle.EmpleadoAsignado, detalle.NombreProyecto, solicitud.CausaPrincipal, solicitud.AccionSugerida);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "SolicitudRevisionOperativa", solicitud.ID, "SolicitarRevisionSobrecalificacion", $"Se derivó al PM la revisión de una asignación sobrecalificada de '{detalle.EmpleadoAsignado}' en la tarea '{detalle.Titulo}'.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia", "Proyectos"));
    }

    private List<ContextoProyectoAnalitico> ConsultarContextos(Usuario usuario, int? idProyecto, int? idCliente = null)
    {
        int? idPM = EsPMSinRolDueno(usuario) ? _analiticaMPP.ConsultarIdPM(usuario) : null;
        FiltroRiesgoRetraso filtro = new FiltroRiesgoRetraso(idProyecto, idCliente, idPM, null, null);
        List<Proyecto> proyectos = _riesgoMPP.ConsultarProyectosActivos(usuario, filtro);
        List<ContextoProyectoAnalitico> contextos = new List<ContextoProyectoAnalitico>();
        foreach (Proyecto proyecto in proyectos)
        {
            ContextoProyectoAnalitico contexto = new ContextoProyectoAnalitico();
            contexto.Proyecto = proyecto;
            contexto.Tareas = _riesgoMPP.ConsultarTareasProyecto(proyecto);
            contexto.Registros = _riesgoMPP.ConsultarRegistrosHora(proyecto);
            contexto.Equipo = _riesgoMPP.ConsultarEquipoProyecto(proyecto);
            contextos.Add(contexto);
        }
        return contextos;
    }

    private List<AsignacionTarea> ConsultarAsignacionesActivas(IEnumerable<ContextoProyectoAnalitico> contextos, Usuario usuario)
    {
        return _asignacionMPP.ConsultarActivas(contextos.SelectMany(contexto => contexto.Tareas).Select(tarea => tarea.ID), usuario);
    }

    private static List<AsignacionTarea> ObtenerDistribucionTarea(Tarea tarea, IEnumerable<AsignacionTarea> asignaciones)
    {
        List<AsignacionTarea> activas = asignaciones.Where(asignacion => asignacion.IdTarea == tarea.ID && asignacion.Activa).ToList();
        if (activas.Count > 0) { return activas; }
        if (!tarea.IdEmpleadoAsignado.HasValue || tarea.HorasEstimadas <= 0) { return new List<AsignacionTarea>(); }

        // Compatibilidad temporal para tareas creadas antes de habilitar la distribución.
        return new List<AsignacionTarea>
        {
            new AsignacionTarea(tarea)
        };
    }

    private void ValidarDueno(Usuario usuario)
    {
        ValidarPermisoEjecutivo(usuario);
        if (!usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase))) { throw new UnauthorizedAccessException("Este análisis está disponible únicamente para el rol Dueño."); }
    }

    private void ValidarPMODueno(Usuario usuario)
    {
        ValidarPermisoEjecutivo(usuario);
        bool autorizado = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase) || string.Equals(rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase));
        if (!autorizado) { throw new UnauthorizedAccessException("Este análisis está disponible para Dueños y PM."); }
    }

    private void ValidarPermisoEjecutivo(Usuario usuario)
    {
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        if (!_rolBLL.TienePermiso(usuario, "ConsultarTableroEjecutivo")) { throw new UnauthorizedAccessException("No tenés permiso para consultar análisis operativos."); }
    }

    private static bool EsPMSinRolDueno(Usuario usuario)
    {
        bool esPM = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase));
        bool esDueno = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase));
        return esPM && !esDueno;
    }

    private static decimal CalcularPorcentaje(decimal valor, decimal baseCalculo)
    {
        return baseCalculo <= 0 ? 0 : Math.Round(valor * 100 / baseCalculo, 2);
    }

    private static void CalcularBalanceHoras(decimal horasEstimadas, decimal horasReales, out decimal horasRestantes, out decimal horasExcedidas)
    {
        horasRestantes = Math.Max(horasEstimadas - horasReales, 0);
        horasExcedidas = Math.Max(horasReales - horasEstimadas, 0);
    }

    private static string ObtenerNombreEmpleado(ContextoProyectoAnalitico contexto, int? idEmpleado)
    {
        Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == idEmpleado);
        return empleado is null ? "Sin asignar" : empleado.Nombre;
    }

    private static void ValidarFiltroOcupacion(FiltroOcupacionOperativa filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date) { throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta."); }
        if (!string.IsNullOrWhiteSpace(filtro.EstadoCarga) && !new[] { "Subutilizado", "Balanceado", "Sobrecargado", "Crítico" }.Contains(filtro.EstadoCarga, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("El estado de carga seleccionado no es válido."); }
    }

    private static List<Tarea> ObtenerTareasOcupacion(ContextoProyectoAnalitico contexto, FiltroOcupacionOperativa filtro)
    {
        return contexto.Tareas.Where(tarea => tarea.Activo && (!filtro.IdEmpleado.HasValue || tarea.IdEmpleadoAsignado == filtro.IdEmpleado.Value) && CumplePeriodoOcupacion(tarea.FechaInicio, tarea.Deadline, filtro)).ToList();
    }

    private static bool CumplePeriodoOcupacion(DateTime fecha, FiltroOcupacionOperativa filtro)
    {
        if (filtro.Desde.HasValue && fecha.Date < filtro.Desde.Value.Date) { return false; }
        if (filtro.Hasta.HasValue && fecha.Date > filtro.Hasta.Value.Date) { return false; }
        return true;
    }

    private static bool CumplePeriodoOcupacion(DateTime? fechaInicio, DateTime? deadline, FiltroOcupacionOperativa filtro)
    {
        if (filtro.Desde.HasValue && deadline.HasValue && deadline.Value.Date < filtro.Desde.Value.Date) { return false; }
        if (filtro.Hasta.HasValue && fechaInicio.HasValue && fechaInicio.Value.Date > filtro.Hasta.Value.Date) { return false; }
        return true;
    }

    private static string ObtenerEstadoOcupacion(decimal porcentaje)
    {
        if (porcentaje < 50) { return "Subutilizado"; }
        if (porcentaje < 86) { return "Balanceado"; }
        if (porcentaje <= 100) { return "Sobrecargado"; }
        return "Crítico";
    }

    private static List<OcupacionProyecto> CrearDistribucionProyectos(List<ContextoProyectoAnalitico> contextos, decimal horasTotales, HashSet<int> empleadosIncluidos, IReadOnlyDictionary<int, decimal> horasPlanificadasPorTarea, IReadOnlyDictionary<(int IdTarea, int IdEmpleado), decimal> horasPlanificadasPorTareaEmpleado)
    {
        List<OcupacionProyecto> proyectos = new List<OcupacionProyecto>();
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            List<Tarea> tareas = contexto.Tareas.Where(tarea => horasPlanificadasPorTarea.ContainsKey(tarea.ID) && horasPlanificadasPorTareaEmpleado.Keys.Any(item => item.IdTarea == tarea.ID && empleadosIncluidos.Contains(item.IdEmpleado))).ToList();
            if (tareas.Count == 0) { continue; }
            OcupacionProyecto proyecto = new OcupacionProyecto();
            proyecto.IdProyecto = contexto.Proyecto.ID;
            proyecto.IdCliente = contexto.Proyecto.IdCliente;
            proyecto.IdPMResponsable = contexto.Proyecto.IdPMResponsable;
            proyecto.NombreProyecto = contexto.Proyecto.Nombre;
            proyecto.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
            proyecto.HorasEstimadasActivas = tareas.Sum(tarea => horasPlanificadasPorTarea[tarea.ID]);
            proyecto.PorcentajeSobreCargaAgencia = CalcularPorcentaje(proyecto.HorasEstimadasActivas, horasTotales);
            foreach (IGrouping<int, KeyValuePair<(int IdTarea, int IdEmpleado), decimal>> grupo in horasPlanificadasPorTareaEmpleado.Where(item => empleadosIncluidos.Contains(item.Key.IdEmpleado) && tareas.Any(tarea => tarea.ID == item.Key.IdTarea)).GroupBy(item => item.Key.IdEmpleado))
            {
                Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == grupo.Key);
                OcupacionEmpleadoProyecto empleadoProyecto = new OcupacionEmpleadoProyecto();
                empleadoProyecto.IdEmpleado = grupo.Key;
                empleadoProyecto.NombreEmpleado = empleado?.Nombre ?? "Sin asignar";
                empleadoProyecto.HorasEstimadas = grupo.Sum(item => item.Value);
                empleadoProyecto.PorcentajeSobreCargaProyecto = CalcularPorcentaje(empleadoProyecto.HorasEstimadas, proyecto.HorasEstimadasActivas);
                proyecto.Empleados.Add(empleadoProyecto);
            }
            proyecto.Empleados = proyecto.Empleados.OrderByDescending(item => item.HorasEstimadas).ToList();
            proyectos.Add(proyecto);
        }
        return proyectos.OrderByDescending(proyecto => proyecto.HorasEstimadasActivas).ToList();
    }

    private static List<OcupacionCliente> CrearDistribucionClientes(List<OcupacionProyecto> proyectos, decimal horasTotales)
    {
        List<OcupacionCliente> clientes = new List<OcupacionCliente>();
        foreach (IGrouping<int, OcupacionProyecto> grupo in proyectos.GroupBy(proyecto => proyecto.IdCliente))
        {
            OcupacionCliente cliente = new OcupacionCliente();
            cliente.IdCliente = grupo.Key;
            cliente.NombreCliente = grupo.First().NombreCliente;
            cliente.HorasEstimadasActivas = grupo.Sum(proyecto => proyecto.HorasEstimadasActivas);
            cliente.PorcentajeSobreCargaAgencia = CalcularPorcentaje(cliente.HorasEstimadasActivas, horasTotales);
            cliente.Proyectos = grupo.OrderByDescending(proyecto => proyecto.HorasEstimadasActivas).ToList();
            clientes.Add(cliente);
        }
        return clientes.OrderByDescending(cliente => cliente.HorasEstimadasActivas).ToList();
    }

    private static void ValidarFiltroDesvio(FiltroDesvioHoras filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date) { throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta."); }
        if (!string.IsNullOrWhiteSpace(filtro.NivelDesvio) && !new[] { "Dentro de lo previsto", "En seguimiento", "Excedido" }.Contains(filtro.NivelDesvio, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("El nivel de desvío seleccionado no es válido."); }
    }

    private static bool CumpleFiltroTarea(Tarea tarea, FiltroDesvioHoras filtro)
    {
        if (filtro.IdTarea.HasValue && tarea.ID != filtro.IdTarea.Value) { return false; }
        if (filtro.IdEmpleado.HasValue && tarea.IdEmpleadoAsignado != filtro.IdEmpleado.Value) { return false; }
        if (!string.IsNullOrWhiteSpace(filtro.EstadoTarea) && !string.Equals(tarea.Estado, filtro.EstadoTarea, StringComparison.OrdinalIgnoreCase)) { return false; }
        return true;
    }

    private static bool CumplePeriodo(RegistroHora registro, FiltroDesvioHoras filtro)
    {
        if (filtro.Desde.HasValue && registro.Fecha.Date < filtro.Desde.Value.Date) { return false; }
        if (filtro.Hasta.HasValue && registro.Fecha.Date > filtro.Hasta.Value.Date) { return false; }
        return true;
    }

    private static bool CumpleNivelDesvio(string estado, string? nivelDesvio)
    {
        return string.IsNullOrWhiteSpace(nivelDesvio) || string.Equals(estado, nivelDesvio, StringComparison.OrdinalIgnoreCase);
    }

    private static string ObtenerEstadoDesvio(decimal porcentajeConsumo)
    {
        if (porcentajeConsumo > 100) { return "Excedido"; }
        if (porcentajeConsumo >= 80) { return "En seguimiento"; }
        return "Dentro de lo previsto";
    }

    private static void ValidarFiltroConveniencia(FiltroConvenienciaOperativa filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date) { throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta."); }
        if (!string.IsNullOrWhiteSpace(filtro.NivelDesvioCosto) && !new[] { "Dentro del presupuesto", "Costo excedido", "Costo real incompleto", "Sin datos de planificación" }.Contains(filtro.NivelDesvioCosto, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El nivel de costo seleccionado no es válido.");
        }
    }

    private static bool CumplePeriodoConveniencia(Tarea tarea, FiltroConvenienciaOperativa filtro)
    {
        if (filtro.Desde.HasValue && tarea.Deadline.HasValue && tarea.Deadline.Value.Date < filtro.Desde.Value.Date) { return false; }
        if (filtro.Hasta.HasValue && tarea.FechaInicio.HasValue && tarea.FechaInicio.Value.Date > filtro.Hasta.Value.Date) { return false; }
        return true;
    }

    private static bool CumplePeriodo(RegistroHora registro, DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && registro.Fecha.Date < desde.Value.Date) { return false; }
        if (hasta.HasValue && registro.Fecha.Date > hasta.Value.Date) { return false; }
        return true;
    }

    private static bool CumpleNivelConveniencia(ConvenienciaOperativaProyecto proyecto, string? nivel)
    {
        return string.IsNullOrWhiteSpace(nivel) || string.Equals(proyecto.EstadoConveniencia, nivel, StringComparison.OrdinalIgnoreCase);
    }

    private static ConvenienciaOperativaProyecto ConstruirConvenienciaProyecto(ContextoProyectoAnalitico contexto, List<Tarea> tareas, List<RegistroHora> registros, IEnumerable<AsignacionTarea> asignaciones, IReadOnlyDictionary<int, Empleado> empleados, out List<ComposicionCostoOperativo> composicion)
    {
        ConvenienciaOperativaProyecto detalle = new ConvenienciaOperativaProyecto
        {
            IdProyecto = contexto.Proyecto.ID,
            IdCliente = contexto.Proyecto.IdCliente,
            NombreProyecto = contexto.Proyecto.Nombre,
            NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre",
            HorasEstimadas = tareas.Sum(tarea => tarea.HorasEstimadas),
            HorasReales = registros.Sum(registro => registro.CantidadHoras)
        };
        Dictionary<(int IdTarea, int IdEmpleado), ComposicionCostoOperativo> componentes = new Dictionary<(int IdTarea, int IdEmpleado), ComposicionCostoOperativo>();
        Dictionary<int, Tarea> tareasPorId = tareas.ToDictionary(tarea => tarea.ID);

        foreach (Tarea tarea in tareas)
        {
            foreach (AsignacionTarea asignacion in ObtenerDistribucionTarea(tarea, asignaciones))
            {
                Empleado? empleado = empleados.GetValueOrDefault(asignacion.IdEmpleado);
                ComposicionCostoOperativo componente = ObtenerComponente(componentes, tarea, asignacion.IdEmpleado, empleado);
                componente.HorasPlanificadas += asignacion.HorasAsignadas;
                if (empleado is null || empleado.CostoHora <= 0)
                {
                    componente.CostoNoConfigurado = true;
                    detalle.HorasPlanificadasSinCosto += asignacion.HorasAsignadas;
                }
                else { componente.CostoPlanificado += asignacion.HorasAsignadas * empleado.CostoHora; detalle.CostoPlanificado += asignacion.HorasAsignadas * empleado.CostoHora; }
            }
        }

        foreach (RegistroHora registro in registros)
        {
            if (!tareasPorId.TryGetValue(registro.IdTarea, out Tarea? tarea)) { continue; }
            Empleado? empleado = empleados.GetValueOrDefault(registro.IdEmpleado);
            ComposicionCostoOperativo componente = ObtenerComponente(componentes, tarea, registro.IdEmpleado, empleado);
            componente.HorasReales += registro.CantidadHoras;
            if (empleado is null || empleado.CostoHora <= 0)
            {
                componente.CostoNoConfigurado = true;
                detalle.HorasRealesSinCosto += registro.CantidadHoras;
            }
            else { componente.CostoReal += registro.CantidadHoras * empleado.CostoHora; detalle.CostoReal += registro.CantidadHoras * empleado.CostoHora; }
        }

        detalle.DesvioCosto = detalle.CostoReal - detalle.CostoPlanificado;
        detalle.PresupuestoNoConsumido = Math.Max(detalle.CostoPlanificado - detalle.CostoReal, 0);
        detalle.Sobrecosto = Math.Max(detalle.CostoReal - detalle.CostoPlanificado, 0);
        detalle.CostoRealIncompleto = detalle.HorasRealesSinCosto > 0 || detalle.HorasPlanificadasSinCosto > 0;
        detalle.EstadoConveniencia = ObtenerEstadoConveniencia(detalle);
        detalle.FactorPrincipal = ObtenerFactorConveniencia(contexto, detalle);
        composicion = componentes.Values.ToList();
        return detalle;
    }

    private static ComposicionCostoOperativo ObtenerComponente(Dictionary<(int IdTarea, int IdEmpleado), ComposicionCostoOperativo> componentes, Tarea tarea, int idEmpleado, Empleado? empleado)
    {
        (int IdTarea, int IdEmpleado) clave = (tarea.ID, idEmpleado);
        if (componentes.TryGetValue(clave, out ComposicionCostoOperativo? existente)) { return existente; }
        ComposicionCostoOperativo componente = new ComposicionCostoOperativo
        {
            IdTarea = tarea.ID,
            IdEmpleado = idEmpleado,
            Tarea = tarea.Titulo,
            Empleado = empleado?.Nombre ?? "Empleado sin costo configurado",
            Seniority = empleado?.Seniority ?? "Sin datos",
            CostoHora = empleado?.CostoHora ?? 0,
            CostoNoConfigurado = empleado is null || empleado.CostoHora <= 0
        };
        componentes.Add(clave, componente);
        return componente;
    }

    private static List<ConvenienciaOperativaCliente> CrearResumenClientes(List<ConvenienciaOperativaProyecto> proyectos, HashSet<int> clientesEnRevision)
    {
        List<ConvenienciaOperativaCliente> clientes = new List<ConvenienciaOperativaCliente>();
        foreach (IGrouping<int, ConvenienciaOperativaProyecto> grupo in proyectos.GroupBy(proyecto => proyecto.IdCliente))
        {
            ConvenienciaOperativaCliente cliente = new ConvenienciaOperativaCliente
            {
                IdCliente = grupo.Key,
                NombreCliente = grupo.First().NombreCliente,
                HorasEstimadas = grupo.Sum(proyecto => proyecto.HorasEstimadas),
                HorasReales = grupo.Sum(proyecto => proyecto.HorasReales),
                CostoPlanificado = grupo.Sum(proyecto => proyecto.CostoPlanificado),
                CostoReal = grupo.Sum(proyecto => proyecto.CostoReal),
                HorasRealesSinCosto = grupo.Sum(proyecto => proyecto.HorasRealesSinCosto),
                EnRevisionOperativa = clientesEnRevision.Contains(grupo.Key),
                Proyectos = grupo.OrderByDescending(proyecto => proyecto.Sobrecosto).ThenBy(proyecto => proyecto.NombreProyecto).ToList()
            };
            cliente.DesvioCosto = cliente.CostoReal - cliente.CostoPlanificado;
            cliente.PresupuestoNoConsumido = Math.Max(cliente.CostoPlanificado - cliente.CostoReal, 0);
            cliente.Sobrecosto = Math.Max(cliente.CostoReal - cliente.CostoPlanificado, 0);
            cliente.EstadoConveniencia = cliente.Sobrecosto > 0 ? "Costo excedido" : cliente.HorasRealesSinCosto > 0 ? "Costo real incompleto" : cliente.CostoPlanificado <= 0 ? "Sin datos de planificación" : "Dentro del presupuesto";
            clientes.Add(cliente);
        }
        return clientes.OrderByDescending(cliente => cliente.Sobrecosto).ThenByDescending(cliente => cliente.HorasRealesSinCosto).ThenBy(cliente => cliente.NombreCliente).ToList();
    }

    private static string ObtenerEstadoConveniencia(ConvenienciaOperativaProyecto detalle)
    {
        if (detalle.CostoPlanificado <= 0) { return "Sin datos de planificación"; }
        if (detalle.Sobrecosto > 0) { return "Costo excedido"; }
        if (detalle.CostoRealIncompleto) { return "Costo real incompleto"; }
        return "Dentro del presupuesto";
    }

    private static string ObtenerFactorConveniencia(ContextoProyectoAnalitico contexto, ConvenienciaOperativaProyecto detalle)
    {
        if (detalle.Sobrecosto > 0) { return "El costo operativo registrado supera el presupuesto interno disponible."; }
        if (detalle.HorasRealesSinCosto > 0) { return "Hay " + detalle.HorasRealesSinCosto.ToString("N2") + " h registradas por perfiles sin costo interno configurado."; }
        if (detalle.HorasPlanificadasSinCosto > 0) { return "Hay horas planificadas sobre perfiles sin costo interno configurado."; }
        if (contexto.Tareas.Any(tarea => tarea.Bloqueada)) { return "Hay tareas bloqueadas que pueden aumentar el desgaste operativo."; }
        return "El costo registrado se mantiene dentro del presupuesto interno planificado.";
    }

    private static int ObtenerNivelSeniority(string seniority)
    {
        string valor = seniority.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        if (valor.Contains("trainee")) { return 1; }
        if (valor.Contains("junior")) { return 2; }
        if (valor.Contains("semi") || valor.Contains("ssr")) { return 3; }
        if (valor.Contains("senior") || valor == "sr") { return 4; }
        if (valor.Contains("lead") || valor.Contains("especialista")) { return 5; }
        return 0;
    }

    private static string ObtenerSeniorityRequerido(Tarea tarea)
    {
        if (!string.IsNullOrWhiteSpace(tarea.SeniorityRequerido)) { return tarea.SeniorityRequerido; }

        string complejidad = tarea.Complejidad?.Trim().ToLowerInvariant() ?? string.Empty;
        if (complejidad == "alta") { return "Senior"; }
        if (complejidad == "media") { return "Semisenior"; }
        return "Junior";
    }

    private sealed class ContextoProyectoAnalitico
    {
        public Proyecto Proyecto { get; set; } = new Proyecto();
        public List<Tarea> Tareas { get; set; } = new List<Tarea>();
        public List<RegistroHora> Registros { get; set; } = new List<RegistroHora>();
        public List<Empleado> Equipo { get; set; } = new List<Empleado>();
    }
}
