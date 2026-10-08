using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRecursos
{
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLRecursos(MPPRecursos recursosMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _recursosMPP = recursosMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<Skill> ConsultarSkills(Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            List<Skill> skills = _recursosMPP.ConsultarSkills();
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AreaSkill> ConsultarAreasSkills(Usuario solicitante)
    {
        ValidarGestionUsuarios(solicitante);
        return _recursosMPP.ConsultarAreasSkills();
    }

    public Skill RegistrarSkill(Skill skill, Usuario solicitante)
    {
        ValidarGestionUsuarios(solicitante);
        if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); }
        if (!skill.IdAreaSkill.HasValue || !_recursosMPP.ConsultarAreasSkills().Any(area => area.ID == skill.IdAreaSkill.Value && area.Activo)) { throw new ArgumentException("Seleccioná un área válida para la skill."); }
        skill.Nombre = skill.Nombre.Trim();
        return _recursosMPP.RegistrarSkill(skill);
    }

    public bool CambiarEstadoSkill(Skill skill, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            bool resultado = _recursosMPP.CambiarEstadoSkill(skill);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Empleado ConsultarFichaEmpleado(Usuario usuario, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            ObtenerAgencia(solicitante);
            Empleado empleado = _recursosMPP.ConsultarFichaEmpleado(usuario, solicitante);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void RegistrarExportacionFichaEmpleado(int idUsuario, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            if (idUsuario <= 0) { throw new ArgumentException("Seleccioná un empleado válido."); }

            Empleado empleado = _recursosMPP.ConsultarFichaEmpleado(new Usuario { ID = idUsuario }, solicitante);
            _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Empleado", empleado.ID, "ExportarLegajoEmpleado", "Se preparó la exportación del legajo de un empleado.", "Exitoso", "Información", "Recursos"));
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarFichaEmpleado(Empleado empleado, Usuario solicitante)
    {
        ValidarGestionUsuarios(solicitante);
        ObtenerAgencia(solicitante);
        if (empleado.ID <= 0)
        {
            throw new ArgumentException("Seleccioná un empleado válido.");
        }
        if (empleado.EmpleadoSkills is null || empleado.EmpleadoSkills.Count == 0)
        {
            throw new ArgumentException("Seleccioná al menos una skill para el empleado.");
        }
        if (empleado.EmpleadoSkills.Any(skill => skill.IdSkill <= 0))
        {
            throw new ArgumentException("Una de las skills seleccionadas no es válida.");
        }
        if (empleado.DisponibilidadBase is null || empleado.DisponibilidadBase.HorasSemanales <= 0)
        {
            throw new ArgumentException("Completá una disponibilidad semanal válida para el empleado.");
        }
        if (empleado.DisponibilidadBase.HoraInicio >= empleado.DisponibilidadBase.HoraFin)
        {
            throw new ArgumentException("La hora de fin debe ser posterior a la hora de inicio.");
        }
        _recursosMPP.GuardarFichaEmpleado(empleado, solicitante);
    }

    public Empleado ConsultarMiDisponibilidad(Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            Empleado empleado = _recursosMPP.ConsultarMiDisponibilidad(usuario);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarMiDisponibilidad(DisponibilidadBase disponibilidad, Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            if (disponibilidad.HorasSemanales <= 0) { throw new ArgumentException("Ingresá una disponibilidad semanal válida."); }
            if (disponibilidad.HoraFin <= disponibilidad.HoraInicio) { throw new ArgumentException("La hora de finalización debe ser posterior a la de inicio."); }
            _recursosMPP.GuardarMiDisponibilidad(disponibilidad, usuario);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AusenciaEmpleado> ConsultarMisAusencias(Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarMisAusencias(usuario);
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public ResultadoGestionDisponibilidad RegistrarAusencia(AusenciaEmpleado ausencia, Usuario usuario)
    {
        try
        {
            ValidarEmpleadoSolicitante(usuario);
            ValidarDatosAusencia(ausencia);
            int idEmpleado = _recursosMPP.ConsultarIdEmpleadoPorUsuarioAgencia(usuario.ID, usuario);
            ValidarSolapamiento(idEmpleado, ausencia.FechaInicioSolicitada, ausencia.FechaFinSolicitada, null, usuario);
            List<TareaDisponibilidadAfectada> tareasAfectadas = _recursosMPP.ConsultarTareasAfectadas(idEmpleado, ausencia.FechaInicioSolicitada, ausencia.FechaFinSolicitada, usuario);
            AusenciaEmpleado resultado = _recursosMPP.RegistrarAusencia(ausencia, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "AusenciaEmpleado", resultado.ID, "Solicitar no disponibilidad", "El empleado registró una solicitud pendiente de aprobación.", "Exitoso", "Información", "Recursos"));
            return new ResultadoGestionDisponibilidad { Ausencia = resultado, TareasAfectadas = tareasAfectadas };
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AusenciaEmpleado> ConsultarSolicitudesPendientes(Usuario solicitante)
    {
        try { ValidarGestorDisponibilidad(solicitante); return _recursosMPP.ConsultarSolicitudesPendientes(solicitante); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<EmpleadoDisponibilidadOpcion> ConsultarEmpleadosDisponibilidad(Usuario solicitante)
    {
        try { ValidarGestorDisponibilidad(solicitante); return _recursosMPP.ConsultarEmpleadosDisponibilidad(solicitante); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public ResultadoGestionDisponibilidad AprobarAusencia(ResolucionAusenciaEmpleado resolucion, Usuario solicitante, bool parcial)
    {
        try
        {
            ValidarGestorDisponibilidad(solicitante);
            AusenciaEmpleado solicitud = _recursosMPP.ConsultarAusencia(resolucion.IdAusencia, solicitante);
            ValidarSolicitudPendiente(solicitud);
            resolucion.FechaInicioAprobada ??= solicitud.FechaInicioSolicitada;
            resolucion.FechaFinAprobada ??= solicitud.FechaFinSolicitada;
            resolucion.HorasNoDisponiblesAprobadas ??= solicitud.HorasNoDisponiblesSolicitadas;
            if (resolucion.FechaInicioAprobada.Value.Date < solicitud.FechaInicioSolicitada.Date || resolucion.FechaFinAprobada.Value.Date > solicitud.FechaFinSolicitada.Date || resolucion.FechaFinAprobada.Value.Date < resolucion.FechaInicioAprobada.Value.Date) { throw new ArgumentException("El período aprobado debe estar dentro del período solicitado."); }
            if (parcial && string.IsNullOrWhiteSpace(resolucion.MotivoResolucion)) { throw new ArgumentException("Ingresá una observación para la aprobación parcial."); }
            ValidarSolapamiento(solicitud.IdEmpleado, resolucion.FechaInicioAprobada.Value, resolucion.FechaFinAprobada.Value, solicitud.ID, solicitante);
            List<TareaDisponibilidadAfectada> tareas = _recursosMPP.ConsultarTareasAfectadas(solicitud.IdEmpleado, resolucion.FechaInicioAprobada.Value, resolucion.FechaFinAprobada.Value, solicitante);
            string estado = parcial ? "Aprobada parcialmente" : "Aprobada";
            AusenciaEmpleado resultado = _recursosMPP.ResolverAusencia(resolucion, estado, solicitante);
            _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "AusenciaEmpleado", resultado.ID, parcial ? "Aprobar parcialmente no disponibilidad" : "Aprobar no disponibilidad", "Se actualizó la disponibilidad operativa del período autorizado.", "Exitoso", "Información", "Recursos"));
            return new ResultadoGestionDisponibilidad { Ausencia = resultado, TareasAfectadas = tareas };
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public AusenciaEmpleado RechazarAusencia(ResolucionAusenciaEmpleado resolucion, Usuario solicitante)
    {
        try
        {
            ValidarGestorDisponibilidad(solicitante);
            if (string.IsNullOrWhiteSpace(resolucion.MotivoResolucion)) { throw new ArgumentException("Ingresá el motivo del rechazo."); }
            AusenciaEmpleado solicitud = _recursosMPP.ConsultarAusencia(resolucion.IdAusencia, solicitante);
            ValidarSolicitudPendiente(solicitud);
            AusenciaEmpleado resultado = _recursosMPP.ResolverAusencia(resolucion, "Rechazada", solicitante);
            _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "AusenciaEmpleado", resultado.ID, "Rechazar no disponibilidad", "La solicitud fue rechazada sin modificar la disponibilidad operativa.", "Exitoso", "Información", "Recursos"));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public ResultadoGestionDisponibilidad RegistrarAusenciaDirecta(AusenciaEmpleado ausencia, Usuario solicitante)
    {
        try
        {
            ValidarGestorDisponibilidad(solicitante); ValidarDatosAusencia(ausencia);
            if (ausencia.IdEmpleado <= 0) { throw new ArgumentException("Seleccioná un empleado activo."); }
            ausencia.IdEmpleado = _recursosMPP.ConsultarIdEmpleadoPorUsuarioAgencia(ausencia.IdEmpleado, solicitante);
            ValidarSolapamiento(ausencia.IdEmpleado, ausencia.FechaInicioSolicitada, ausencia.FechaFinSolicitada, null, solicitante);
            List<TareaDisponibilidadAfectada> tareas = _recursosMPP.ConsultarTareasAfectadas(ausencia.IdEmpleado, ausencia.FechaInicioSolicitada, ausencia.FechaFinSolicitada, solicitante);
            AusenciaEmpleado resultado = _recursosMPP.RegistrarAusenciaDirecta(ausencia, solicitante);
            _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "AusenciaEmpleado", resultado.ID, "Registrar no disponibilidad directa", "Se registró y aprobó directamente un período de no disponibilidad.", "Exitoso", "Información", "Recursos"));
            return new ResultadoGestionDisponibilidad { Ausencia = resultado, TareasAfectadas = tareas };
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarGestionUsuarios(Usuario solicitante)
    {
        try
        {
            if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar empleados."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarDisponibilidad(Usuario usuario)
    {
        try
        {
            if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
            if (!_rolBLL.TienePermiso(usuario, "GestionarDisponibilidad")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar disponibilidad."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarEmpleadoSolicitante(Usuario usuario)
    {
        ValidarDisponibilidad(usuario);
        if (!usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase))) { throw new UnauthorizedAccessException("Sólo un empleado puede registrar su propia solicitud de no disponibilidad."); }
    }

    private void ValidarGestorDisponibilidad(Usuario usuario)
    {
        ValidarDisponibilidad(usuario);
        if (!_recursosMPP.EsGestorDisponibilidad(usuario)) { throw new UnauthorizedAccessException("Sólo un PM autorizado o el Dueño puede gestionar solicitudes de disponibilidad."); }
    }

    private static void ValidarDatosAusencia(AusenciaEmpleado ausencia)
    {
        if (string.IsNullOrWhiteSpace(ausencia.TipoPeriodo) || ausencia.FechaInicioSolicitada == DateTime.MinValue || ausencia.FechaFinSolicitada == DateTime.MinValue || string.IsNullOrWhiteSpace(ausencia.Motivo)) { throw new ArgumentException("Completá tipo, fechas y motivo de la ausencia."); }
        if (ausencia.FechaFinSolicitada.Date < ausencia.FechaInicioSolicitada.Date) { throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial."); }
        if (ausencia.HorasNoDisponiblesSolicitadas.HasValue && ausencia.HorasNoDisponiblesSolicitadas <= 0) { throw new ArgumentException("Las horas no disponibles deben ser mayores a cero."); }
        ausencia.TipoPeriodo = ausencia.TipoPeriodo.Trim(); ausencia.Motivo = ausencia.Motivo.Trim(); ausencia.ComprobanteUrl = string.IsNullOrWhiteSpace(ausencia.ComprobanteUrl) ? null : ausencia.ComprobanteUrl.Trim();
    }

    private void ValidarSolapamiento(int idEmpleado, DateTime desde, DateTime hasta, int? idAusenciaExcluir, Usuario solicitante)
    {
        bool incompatible = _recursosMPP.ConsultarAusenciasEmpleado(idEmpleado, solicitante).Any(ausencia => ausencia.ID != idAusenciaExcluir && ausencia.Activo && ausencia.Estado != "Rechazada" && ausencia.FechaInicioSolicitada.Date <= hasta.Date && ausencia.FechaFinSolicitada.Date >= desde.Date);
        if (incompatible) { throw new ArgumentException("Ya existe un período de no disponibilidad incompatible para esas fechas."); }
    }

    private static void ValidarSolicitudPendiente(AusenciaEmpleado solicitud)
    {
        if (!string.Equals(solicitud.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("La solicitud ya fue resuelta."); }
    }

    private static int ObtenerAgencia(Usuario usuario)
    {
        try
        {
            if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
            int idAgencia = usuario.IdAgencia.Value;
            return idAgencia;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
