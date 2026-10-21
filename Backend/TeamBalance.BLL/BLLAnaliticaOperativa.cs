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
            List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarAusenciasEmpleado(new AusenciaEmpleado { IdEmpleado = empleado.ID }, usuario);
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
            detalle.AccionSugerida = estado == "Sobrecargado" ? "Revisar la distribución de tareas o evaluar un recurso alternativo." : estado == "Subutilizado" ? "Evaluar la asignación de trabajo compatible con su disponibilidad." : "Mantener seguimiento de la carga vigente.";
            detalle.Proyectos = contextos.Where(contexto => contexto.Tareas.Any(tarea => carga.HorasPlanificadasPorTarea.ContainsKey(tarea.ID))).Select(contexto => contexto.Proyecto.Nombre).Distinct().OrderBy(nombre => nombre).ToList();
            resultado.Empleados.Add(detalle);
        }
        resultado.Empleados = resultado.Empleados.OrderByDescending(empleado => empleado.PorcentajeOcupacion).ThenBy(empleado => empleado.Nombre).ToList();
        resultado.HorasDisponiblesSemanales = resultado.Empleados.Sum(empleado => empleado.HorasDisponibles);
        resultado.HorasEstimadasActivas = resultado.Empleados.Sum(empleado => empleado.HorasEstimadasActivas);
        resultado.PorcentajeOcupacionGeneral = CalcularPorcentaje(resultado.HorasEstimadasActivas, resultado.HorasDisponiblesSemanales);
        resultado.CantidadSubutilizados = resultado.Empleados.Count(empleado => empleado.EstadoCarga == "Subutilizado");
        resultado.CantidadBalanceados = resultado.Empleados.Count(empleado => empleado.EstadoCarga == "Balanceado");
        resultado.CantidadSobrecargados = resultado.Empleados.Count(empleado => empleado.EstadoCarga == "Sobrecargado");
        HashSet<int> empleadosIncluidos = resultado.Empleados.Select(empleado => empleado.IdEmpleado).ToHashSet();
        resultado.Proyectos = CrearDistribucionProyectos(contextos, resultado.HorasEstimadasActivas, empleadosIncluidos, horasPlanificadasPorTarea, horasPlanificadasPorTareaEmpleado);
        resultado.Clientes = CrearDistribucionClientes(resultado.Proyectos, resultado.HorasEstimadasActivas);
        return resultado;
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
        List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarAusenciasEmpleado(new AusenciaEmpleado { IdEmpleado = empleadoBase.ID }, usuario);
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

    public List<SolicitudRevisionOperativa> ConsultarSolicitudesRevisionPendientes(Usuario usuario)
    {
        ValidarPMODueno(usuario);
        if (!EsPMSinRolDueno(usuario)) { throw new UnauthorizedAccessException("Las revisiones pendientes se consultan desde la cuenta del PM responsable."); }
        return _analiticaMPP.ConsultarSolicitudesPendientes(usuario, _analiticaMPP.ConsultarIdPM(usuario));
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
                detalle.PorcentajeConsumo = CalcularPorcentaje(horasReales, tarea.HorasEstimadas);
                detalle.Estado = ObtenerEstadoDesvio(detalle.PorcentajeConsumo);
                if (CumpleNivelDesvio(detalle.Estado, filtroAplicado.NivelDesvio)) { proyecto.Tareas.Add(detalle); }
            }
            if (proyecto.Tareas.Count == 0) { continue; }
            proyecto.Tareas = proyecto.Tareas.OrderByDescending(tarea => tarea.PorcentajeConsumo).ThenByDescending(tarea => tarea.DesvioHoras).ToList();
            proyecto.HorasEstimadas = proyecto.Tareas.Sum(tarea => tarea.HorasEstimadas);
            proyecto.HorasReales = proyecto.Tareas.Sum(tarea => tarea.HorasReales);
            proyecto.DesvioHoras = proyecto.HorasReales - proyecto.HorasEstimadas;
            proyecto.PorcentajeConsumo = CalcularPorcentaje(proyecto.HorasReales, proyecto.HorasEstimadas);
            proyecto.Estado = ObtenerEstadoDesvio(proyecto.PorcentajeConsumo);
            resultado.Proyectos.Add(proyecto);
        }
        resultado.Proyectos = resultado.Proyectos.OrderByDescending(proyecto => proyecto.PorcentajeConsumo).ThenByDescending(proyecto => proyecto.DesvioHoras).ToList();
        resultado.HorasEstimadas = resultado.Proyectos.Sum(proyecto => proyecto.HorasEstimadas);
        resultado.HorasReales = resultado.Proyectos.Sum(proyecto => proyecto.HorasReales);
        resultado.DesvioHoras = resultado.HorasReales - resultado.HorasEstimadas;
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
        detalle.PorcentajeConsumo = CalcularPorcentaje(detalle.HorasReales, detalle.HorasEstimadas);
        detalle.EstadoDesvio = ObtenerEstadoDesvio(detalle.PorcentajeConsumo);
        detalle.RegistrosHoras = registros;
        return detalle;
    }

    public void RegistrarAccionAjusteDesvioHoras(Usuario usuario, AccionAjusteDesvioHoras accion)
    {
        ValidarPMODueno(usuario);
        if (accion.IdProyecto <= 0) { throw new ArgumentException("Seleccioná un proyecto para registrar la acción de ajuste."); }
        if (string.IsNullOrWhiteSpace(accion.Observacion)) { throw new ArgumentException("Describí la acción de ajuste antes de confirmarla."); }
        if (accion.Observacion.Trim().Length > 1500) { throw new ArgumentException("La observación no puede superar los 1500 caracteres."); }
        if (accion.IdTarea.HasValue) { ConsultarDetalleDesvioHorasTarea(usuario, accion.IdProyecto, accion.IdTarea.Value, null, null); }
        else if (!ConsultarContextos(usuario, accion.IdProyecto).Any()) { throw new KeyNotFoundException("No existe el proyecto solicitado o no tenés acceso a él."); }
        string entidad = accion.IdTarea.HasValue ? "Tarea" : "Proyecto";
        int idEntidad = accion.IdTarea ?? accion.IdProyecto;
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, entidad, idEntidad, "RegistrarAccionAjusteDesvioHoras", "Acción de ajuste registrada para el desvío de horas: " + accion.Observacion.Trim(), "Exitoso", "Informacion", "Proyectos"));
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

    public AnalisisConvenienciaOperativa ConsultarConveniencia(Usuario usuario)
    {
        ValidarDueno(usuario);
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, null);
        List<AsignacionTarea> asignaciones = ConsultarAsignacionesActivas(contextos, usuario);
        AnalisisConvenienciaOperativa resultado = new AnalisisConvenienciaOperativa();
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            ConvenienciaOperativaProyecto detalle = new ConvenienciaOperativaProyecto();
            detalle.IdProyecto = contexto.Proyecto.ID;
            detalle.NombreProyecto = contexto.Proyecto.Nombre;
            detalle.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
            detalle.HorasEstimadas = contexto.Tareas.Sum(tarea => tarea.HorasEstimadas);
            detalle.HorasReales = contexto.Registros.Sum(registro => registro.CantidadHoras);
            foreach (Tarea tarea in contexto.Tareas)
            {
                foreach (AsignacionTarea asignacion in ObtenerDistribucionTarea(tarea, asignaciones))
                {
                    Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == asignacion.IdEmpleado);
                    if (empleado is not null) { detalle.CostoPlanificado += asignacion.HorasAsignadas * empleado.CostoHora; }
                }
            }
            foreach (RegistroHora registro in contexto.Registros)
            {
                Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == registro.IdEmpleado);
                if (empleado is not null) { detalle.CostoReal += registro.CantidadHoras * empleado.CostoHora; }
            }
            detalle.DesvioCosto = detalle.CostoReal - detalle.CostoPlanificado;
            detalle.EstadoConveniencia = detalle.CostoPlanificado == 0 ? "Sin datos suficientes" : detalle.CostoReal > detalle.CostoPlanificado ? "Revisar" : "Conveniente";
            detalle.FactorPrincipal = ObtenerFactorConveniencia(contexto, detalle);
            resultado.Proyectos.Add(detalle);
        }
        resultado.Proyectos = resultado.Proyectos.OrderByDescending(proyecto => proyecto.DesvioCosto).ThenBy(proyecto => proyecto.NombreCliente).ToList();
        resultado.CostoPlanificado = resultado.Proyectos.Sum(proyecto => proyecto.CostoPlanificado);
        resultado.CostoReal = resultado.Proyectos.Sum(proyecto => proyecto.CostoReal);
        resultado.DesvioCosto = resultado.CostoReal - resultado.CostoPlanificado;
        return resultado;
    }

    public AnalisisSobrecalificacionPerfiles ConsultarSobrecalificacion(Usuario usuario)
    {
        ValidarDueno(usuario);
        List<ContextoProyectoAnalitico> contextos = ConsultarContextos(usuario, null);
        List<AsignacionTarea> asignaciones = ConsultarAsignacionesActivas(contextos, usuario);
        AnalisisSobrecalificacionPerfiles resultado = new AnalisisSobrecalificacionPerfiles();
        foreach (ContextoProyectoAnalitico contexto in contextos)
        {
            foreach (Tarea tarea in contexto.Tareas)
            {
                foreach (AsignacionTarea asignacion in ObtenerDistribucionTarea(tarea, asignaciones))
                {
                    Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == asignacion.IdEmpleado);
                    if (empleado is null || string.IsNullOrWhiteSpace(tarea.SeniorityRequerido) || ObtenerNivelSeniority(empleado.Seniority) <= ObtenerNivelSeniority(tarea.SeniorityRequerido)) { continue; }
                    decimal horasRegistradas = contexto.Registros.Where(registro => registro.IdTarea == tarea.ID && registro.IdEmpleado == empleado.ID).Sum(registro => registro.CantidadHoras);
                    decimal horasAfectadas = horasRegistradas > 0 ? horasRegistradas : asignacion.HorasAsignadas;
                    SobrecalificacionTarea detalle = new SobrecalificacionTarea();
                    detalle.IdTarea = tarea.ID;
                    detalle.IdProyecto = contexto.Proyecto.ID;
                    detalle.Titulo = tarea.Titulo;
                    detalle.NombreProyecto = contexto.Proyecto.Nombre;
                    detalle.NombreCliente = contexto.Proyecto.NombreCliente ?? "Cliente sin nombre";
                    detalle.SeniorityRequerido = tarea.SeniorityRequerido;
                    detalle.SeniorityAsignado = empleado.Seniority;
                    detalle.EmpleadoAsignado = empleado.Nombre;
                    detalle.HorasAfectadas = horasAfectadas;
                    detalle.ImpactoOperativoEstimado = horasAfectadas * empleado.CostoHora;
                    detalle.Nivel = ObtenerNivelSeniority(empleado.Seniority) - ObtenerNivelSeniority(tarea.SeniorityRequerido) > 1 ? "Alto" : "Moderado";
                    resultado.Tareas.Add(detalle);
                }
            }
        }
        resultado.Tareas = resultado.Tareas.OrderByDescending(tarea => tarea.ImpactoOperativoEstimado).ThenByDescending(tarea => tarea.HorasAfectadas).ToList();
        resultado.HorasAfectadas = resultado.Tareas.Sum(tarea => tarea.HorasAfectadas);
        resultado.ImpactoOperativoEstimado = resultado.Tareas.Sum(tarea => tarea.ImpactoOperativoEstimado);
        resultado.CantidadTareasAfectadas = resultado.Tareas.Count;
        return resultado;
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
            new AsignacionTarea { IdTarea = tarea.ID, IdEmpleado = tarea.IdEmpleadoAsignado.Value, HorasAsignadas = tarea.HorasEstimadas, Activa = true }
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

    private static string ObtenerNombreEmpleado(ContextoProyectoAnalitico contexto, int? idEmpleado)
    {
        Empleado? empleado = contexto.Equipo.FirstOrDefault(item => item.ID == idEmpleado);
        return empleado is null ? "Sin asignar" : empleado.Nombre;
    }

    private static void ValidarFiltroOcupacion(FiltroOcupacionOperativa filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date) { throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta."); }
        if (!string.IsNullOrWhiteSpace(filtro.EstadoCarga) && !new[] { "Subutilizado", "Balanceado", "Sobrecargado" }.Contains(filtro.EstadoCarga, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("El estado de carga seleccionado no es válido."); }
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
        if (porcentaje < 70) { return "Subutilizado"; }
        if (porcentaje <= 100) { return "Balanceado"; }
        return "Sobrecargado";
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

    private static string ObtenerFactorConveniencia(ContextoProyectoAnalitico contexto, ConvenienciaOperativaProyecto detalle)
    {
        if (detalle.CostoReal > detalle.CostoPlanificado) { return "El costo operativo real supera al planificado."; }
        if (contexto.Tareas.Any(tarea => tarea.Bloqueada)) { return "Hay tareas bloqueadas que pueden aumentar el desgaste operativo."; }
        return "El consumo registrado se mantiene dentro del costo operativo planificado.";
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

    private sealed class ContextoProyectoAnalitico
    {
        public Proyecto Proyecto { get; set; } = new Proyecto();
        public List<Tarea> Tareas { get; set; } = new List<Tarea>();
        public List<RegistroHora> Registros { get; set; } = new List<RegistroHora>();
        public List<Empleado> Equipo { get; set; } = new List<Empleado>();
    }
}
