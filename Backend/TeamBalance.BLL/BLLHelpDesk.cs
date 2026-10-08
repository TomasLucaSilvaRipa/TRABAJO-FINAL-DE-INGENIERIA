using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed class BLLHelpDesk
{
    private static readonly HashSet<string> Categorias = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Acceso", "Usuarios", "Funcionalidad", "Suscripción y pagos", "Otro" };
    private static readonly HashSet<string> Estados = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pendiente", "En revisión", "Respondida", "Resuelta" };
    private readonly MPPHelpDesk _helpDeskMPP;
    private readonly MPPSuscripcion _suscripcionMPP;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;
    private readonly BLLRol _rolBLL;

    public BLLHelpDesk(MPPHelpDesk helpDeskMPP, MPPSuscripcion suscripcionMPP, BLLBitacora bitacoraBLL, EmailService emailService, BLLRol rolBLL) {
        _helpDeskMPP = helpDeskMPP;
        _suscripcionMPP = suscripcionMPP;
        _bitacoraBLL = bitacoraBLL;
        _emailService = emailService;
        _rolBLL = rolBLL;
    }

    public ConsultaSoporteResumen Crear(Usuario usuario, CrearConsultaSoporteRequest solicitud) {
        ExigirSoportePropio(usuario);
        ValidarSolicitud(solicitud);
        solicitud.IdSuscripcion = usuario.IdAgencia.HasValue ? _suscripcionMPP.ConsultarActual(usuario)?.ID : null;
        ConsultaSoporteResumen consulta = _helpDeskMPP.Crear(usuario, solicitud);
        RegistrarBitacora(usuario, consulta, "CrearConsultaSoporte", "Se registró una consulta de HelpDesk.");
        return consulta;
    }

    public List<ConsultaSoporteResumen> ConsultarPropias(Usuario usuario) { ExigirSoportePropio(usuario); return _helpDeskMPP.ConsultarPropias(usuario); }

    public (ConsultaSoporteResumen Consulta, List<MensajeSoporteDetalle> Mensajes) ConsultarDetalle(Usuario usuario, ConsultaSoporteResumen consulta) {
        ConsultaSoporteResumen consultaAutorizada = ObtenerAutorizada(usuario, consulta);
        return (consultaAutorizada, _helpDeskMPP.ConsultarMensajes(consultaAutorizada));
    }

    public async Task EnviarMensaje(Usuario usuario, EnviarMensajeSoporteRequest solicitud) {
        if (string.IsNullOrWhiteSpace(solicitud.Mensaje) || solicitud.Mensaje.Trim().Length > 4000) throw new ArgumentException("Ingresá un mensaje de hasta 4000 caracteres.");
        ConsultaSoporteResumen consulta = new ConsultaSoporteResumen();
        consulta.ID = solicitud.IdConsulta;
        consulta = ObtenerAutorizada(usuario, consulta);
        bool esSoporte = EsSoporte(usuario);
        if (esSoporte) ExigirBandeja(usuario); else ExigirSoportePropio(usuario);
        string estado = esSoporte ? (string.IsNullOrWhiteSpace(solicitud.Estado) ? "Respondida" : solicitud.Estado.Trim()) : "Pendiente";
        if (!Estados.Contains(estado) || (!esSoporte && estado != "Pendiente")) throw new ArgumentException("El estado indicado no es válido.");
        solicitud.Mensaje = solicitud.Mensaje.Trim();
        consulta.Estado = estado;
        _helpDeskMPP.AgregarMensaje(consulta, usuario, solicitud);
        RegistrarBitacora(usuario, consulta, esSoporte ? "ResponderConsultaSoporte" : "ActualizarConsultaSoporte", esSoporte ? "Soporte respondió una consulta." : "El usuario agregó información a su consulta.");
        if (esSoporte && !string.IsNullOrWhiteSpace(consulta.EmailSolicitante)) await _emailService.EnviarRespuestaSoporte(consulta.EmailSolicitante, consulta.NombreSolicitante, consulta.Asunto, solicitud.Mensaje.Trim());
    }

    public void Cerrar(Usuario usuario, ConsultaSoporteResumen consulta) {
        ExigirBandeja(usuario);
        consulta = ObtenerAutorizada(usuario, consulta);
        consulta.Estado = "Resuelta";
        _helpDeskMPP.CambiarEstado(consulta);
        RegistrarBitacora(usuario, consulta, "ResolverConsultaSoporte", "Soporte marcó una consulta como resuelta.");
    }

    public void CambiarEstado(Usuario usuario, CambiarEstadoConsultaRequest solicitud) {
        ExigirBandeja(usuario);
        string estadoNormalizado = solicitud.Estado?.Trim() ?? string.Empty;
        if (!Estados.Contains(estadoNormalizado)) throw new ArgumentException("El estado indicado no es válido.");
        ConsultaSoporteResumen consulta = new ConsultaSoporteResumen();
        consulta.ID = solicitud.IdConsulta;
        consulta = ObtenerAutorizada(usuario, consulta);
        consulta.Estado = estadoNormalizado;
        _helpDeskMPP.CambiarEstado(consulta);
        RegistrarBitacora(usuario, consulta, "ActualizarEstadoConsultaSoporte", $"Soporte actualizó el estado a {estadoNormalizado}.");
    }

    public List<ConsultaSoporteResumen> Bandeja(Usuario usuario, ConsultaSoporteResumen filtro) {
        ExigirBandeja(usuario);
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && !Estados.Contains(filtro.Estado)) throw new ArgumentException("El filtro de estado no es válido.");
        return _helpDeskMPP.ConsultarBandeja(filtro);
    }

    private ConsultaSoporteResumen ObtenerAutorizada(Usuario usuario, ConsultaSoporteResumen consulta) {
        ConsultaSoporteResumen consultaAutorizada = _helpDeskMPP.ConsultarDetalle(consulta) ?? throw new KeyNotFoundException("La consulta indicada no existe.");
        if (EsSoporte(usuario)) ExigirBandeja(usuario);
        else { ExigirSoportePropio(usuario); if (consultaAutorizada.IdUsuario != usuario.ID) throw new UnauthorizedAccessException("Sólo podés consultar tus propios tickets de soporte."); }
        return consultaAutorizada;
    }

    private static void ValidarSolicitud(CrearConsultaSoporteRequest solicitud)
    {
        if (!Categorias.Contains(solicitud.Categoria ?? string.Empty)) throw new ArgumentException("Seleccioná una categoría válida.");
        if (string.IsNullOrWhiteSpace(solicitud.Asunto) || solicitud.Asunto.Trim().Length > 200) throw new ArgumentException("Ingresá un asunto de hasta 200 caracteres.");
        if (string.IsNullOrWhiteSpace(solicitud.Descripcion) || solicitud.Descripcion.Trim().Length > 4000) throw new ArgumentException("Describí la consulta en hasta 4000 caracteres.");
        solicitud.Categoria = solicitud.Categoria!.Trim(); solicitud.Asunto = solicitud.Asunto!.Trim(); solicitud.Descripcion = solicitud.Descripcion!.Trim();
    }

    private static bool EsSoporte(Usuario usuario) => usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
    private void ExigirSoportePropio(Usuario usuario) { if (!_rolBLL.TienePermiso(usuario, "GestionarSoporte")) throw new UnauthorizedAccessException("No tenés permiso para gestionar consultas de soporte."); }
    private void ExigirBandeja(Usuario usuario) { if (!EsSoporte(usuario) || !_rolBLL.TienePermiso(usuario, "GestionarBandejaSoporte")) throw new UnauthorizedAccessException("Esta bandeja sólo está disponible para operadores de Soporte autorizados."); }
    private void RegistrarBitacora(Usuario usuario, ConsultaSoporteResumen consulta, string accion, string mensaje) => _bitacoraBLL.Add(new Bitacora(usuario.ID, consulta.IdAgencia, "ConsultaSoporte", consulta.ID, accion, mensaje, "Exitoso", "Informacion", "HelpDesk"));
}
