using System.Net.Mail;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLAgencia
{
    private readonly MPPAgencia _agenciaMPP;
    private readonly MPPUsuario _usuarioMPP;
    private readonly ContratacionBLL _contratacionBLL;
    private readonly BLLUsuario _usuarioBLL;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;
    private readonly BLLProyecto _proyectoBLL;
    private readonly BLLNovedades _novedadesBLL;
    private readonly BLLTelegram _telegramBLL;
    private readonly Seguridad _seguridad;

    public BLLAgencia(MPPAgencia agenciaMPP, MPPUsuario usuarioMPP, ContratacionBLL contratacionBLL, BLLUsuario usuarioBLL, BLLRol rolBLL, BLLBitacora bitacoraBLL, EmailService emailService, BLLProyecto proyectoBLL, BLLNovedades novedadesBLL, BLLTelegram telegramBLL, Seguridad seguridad){
        _agenciaMPP = agenciaMPP;
        _usuarioMPP = usuarioMPP;
        _contratacionBLL = contratacionBLL;
        _usuarioBLL = usuarioBLL;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _emailService = emailService;
        _proyectoBLL = proyectoBLL;
        _novedadesBLL = novedadesBLL;
        _telegramBLL = telegramBLL;
        _seguridad = seguridad;
    }

    public async Task<ResultadoRegistroAgenciaValidacion> RegistrarAgencia(Agencia agencia, Usuario usuario, ContratacionServicio contratacionServicio, bool recibirNewsletter, List<int>? categoriasNewsletterIds)
    {
        ValidarDatosRegistro(usuario, contratacionServicio);
        PreferenciasNewsletterRequest preferenciasNewsletter = new PreferenciasNewsletterRequest();
        preferenciasNewsletter.CategoriasIds = categoriasNewsletterIds?.Distinct().ToList() ?? new List<int>();
        if (recibirNewsletter)
        {
            if (preferenciasNewsletter.CategoriasIds.Count == 0) throw new ArgumentException("Seleccioná al menos una categoría para recibir novedades.");
            _novedadesBLL.ValidarPreferencias(preferenciasNewsletter);
        }
        ContratacionServicio contratacion = _contratacionBLL.ConsultarContratacionParaRegistro(contratacionServicio);

        if (contratacion.IdAgencia.HasValue || contratacion.IdUsuario.HasValue)
        {
            if (!contratacion.IdAgencia.HasValue || !contratacion.IdUsuario.HasValue)
            {
                throw new InvalidOperationException("La contratación quedó asociada a un registro incompleto. Contactá a soporte para continuar.");
            }

            if (!string.Equals(usuario.Email.Trim(), contratacion.EmailLaboralResponsable, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La contratación ya fue utilizada para registrar una agencia.");
            }

            agencia.ID = contratacion.IdAgencia.Value;
            usuario.ID = contratacion.IdUsuario.Value;
            usuario.IdAgencia = contratacion.IdAgencia.Value;
            _rolBLL.CargarAutorizacion(usuario);

            if (recibirNewsletter)
            {
                _novedadesBLL.GuardarPreferencias(usuario, preferenciasNewsletter);
            }

            _proyectoBLL.CrearEstadosBase(agencia);
            return CrearSesionValidacionRegistro(usuario);
        }

        agencia.NombreComercial = contratacion.NombreComercialAgencia;
        agencia.RazonSocial = contratacion.RazonSocial;
        agencia.CUIT = contratacion.CUIT;
        agencia.CondicionFiscal = contratacion.CondicionFiscal;
        agencia.EmailContacto = contratacion.EmailFacturacion ?? contratacion.EmailLaboralResponsable;
        agencia.TelefonoContacto = string.IsNullOrWhiteSpace(agencia.TelefonoContacto) ? contratacion.TelefonoContacto : agencia.TelefonoContacto.Trim();
        agencia.FechaAlta = DateTime.Now;
        agencia.Estado = "Activa";
        agencia.Activo = true;

        if (_agenciaMPP.ExisteAgencia(agencia))
        {
            throw new InvalidOperationException("Ya existe una agencia registrada con el CUIT o email de contacto indicado.");
        }

        string email = usuario.Email.Trim().ToLowerInvariant();

        if (!_usuarioBLL.EmailDisponible(usuario))
        {
            throw new InvalidOperationException("Ya existe un usuario registrado con ese email laboral.");
        }
        Rol rol = new Rol("Dueño");
        Rol rolDueño = _rolBLL.ConsultarRolPorNombre(rol);
        if (!usuario.AceptaTerminos) { throw new ArgumentException("Necesitás aceptar los Términos y Condiciones para completar el registro."); }
        _usuarioBLL.PrepararUsuarioDueño(usuario, rolDueño);
        Dueño dueño = _usuarioBLL.CrearDueño();
        ValidacionCuenta validacion = _usuarioBLL.CrearValidacionEmail(out string token);

        RegistroAgenciaResultado registro = _agenciaMPP.RegistrarAgencia(agencia, usuario, dueño, validacion, contratacion);

        agencia.ID = registro.IdAgencia;
        usuario.ID = registro.IdUsuario;
        usuario.IdAgencia = registro.IdAgencia;
        _rolBLL.CargarAutorizacion(usuario);
        if (recibirNewsletter) _novedadesBLL.GuardarPreferencias(usuario, preferenciasNewsletter);
        _proyectoBLL.CrearEstadosBase(agencia);

        Bitacora bitacora = new Bitacora(usuario.ID, agencia.ID,"Agencia",agencia.ID,"RegistrarAgecnia", "Se registró la agencia y el usuario Dueño inicial.","Exitoso","Informacion","Registro");
        _bitacoraBLL.Add(bitacora);
        

        return CrearSesionValidacionRegistro(usuario);
    }

    public bool ValidarCuenta(string token)
    {
        return _usuarioBLL.ValidarCuenta(token);
    }

    public Agencia ConsultarAgencia(Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarAgencia");
        return _agenciaMPP.ConsultarAgencia(ValidarUsuarioConAgencia(solicitante));
    }

    public Agencia ModificarAgencia(Agencia agencia, Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarAgencia");
        if (string.IsNullOrWhiteSpace(agencia.NombreComercial) || string.IsNullOrWhiteSpace(agencia.EmailContacto) || !MailAddress.TryCreate(agencia.EmailContacto.Trim(), out _)) { throw new ArgumentException("Completá un nombre comercial y un email de contacto válido."); }
        agencia.ID = ValidarUsuarioConAgencia(solicitante).IdAgencia.Value;
        agencia.NombreComercial = agencia.NombreComercial.Trim();
        agencia.EmailContacto = agencia.EmailContacto.Trim().ToLowerInvariant();
        Agencia resultado = _agenciaMPP.ModificarAgencia(agencia);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Agencia", resultado.ID, "ModificarAgencia", $"Se modificaron los datos de la agencia {resultado.NombreComercial}.", "Exitoso", "Informacion", "Usuarios"));
        return resultado;
    }

    public Suscripcion? ConsultarSuscripcionActual(Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarSuscripcion");
        return _agenciaMPP.ConsultarSuscripcionActual(ValidarUsuarioConAgencia(solicitante));
    }

    public async Task ReenviarValidacion(Usuario usuario)
    {
        string email = usuario.Email ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out _))
        {
            return;
        }

        usuario = _usuarioBLL.ConsultarUsuarioPendienteValidacion(usuario);

        if (usuario is null)
        {
            return;
        }

        ValidacionCuenta validacion = _usuarioBLL.CrearValidacionEmail(out string token);
        _usuarioBLL.ReemplazarValidacionEmail(usuario, validacion);

        await _emailService.EnviarCorreoValidacion(usuario, token);

        Bitacora bitacora = new Bitacora(0, usuario.ID, usuario.IdAgencia, "ValidacionCuenta", usuario.ID, "ReenviarValidacion", "Se generó un nuevo enlace de validación de correo.", "Exitoso", "Informacion", "Registro", DateTime.Now, null);
        _bitacoraBLL.Add(bitacora);
    }

    public async Task EnviarValidacionEmailRegistro(string tokenGestion)
    {
        Usuario usuario = ObtenerUsuarioPendientePorTokenGestion(tokenGestion);
        ValidacionCuenta validacion = _usuarioBLL.CrearValidacionEmail(out string token);
        _usuarioBLL.ReemplazarValidacionEmail(usuario, validacion);
        bool enviado = await _emailService.EnviarCorreoValidacion(usuario, token);
        if (!enviado) throw new InvalidOperationException("No fue posible enviar el enlace de validación por correo.");
        _bitacoraBLL.Add(new Bitacora(0, usuario.ID, usuario.IdAgencia, "ValidacionCuenta", usuario.ID, "EnviarValidacionEmail", "Se envió la validación inicial por correo electrónico.", "Exitoso", "Informacion", "Registro", DateTime.Now, null));
    }

    public PreparacionTelegram PrepararValidacionTelegramRegistro(string tokenGestion)
    {
        Usuario usuario = ObtenerUsuarioPendientePorTokenGestion(tokenGestion);
        return _telegramBLL.PrepararValidacionRegistro(usuario);
    }

    public EstadoTelegram ConsultarEstadoTelegramRegistro(string tokenGestion)
    {
        Usuario usuario = ObtenerUsuarioPendientePorTokenGestion(tokenGestion);
        return _telegramBLL.ConsultarEstado(usuario);
    }

    public void ConfirmarValidacionTelegramRegistro(string tokenGestion, string codigo)
    {
        Usuario usuario = ObtenerUsuarioPendientePorTokenGestion(tokenGestion);
        _telegramBLL.ConfirmarValidacionRegistro(usuario, codigo);
    }

    public EntregaTelegram ReenviarValidacionTelegramRegistro(string tokenGestion)
    {
        Usuario usuario = ObtenerUsuarioPendientePorTokenGestion(tokenGestion);
        return _telegramBLL.ReenviarCodigoRegistro(usuario);
    }

    private ResultadoRegistroAgenciaValidacion CrearSesionValidacionRegistro(Usuario usuario)
    {
        string tokenGestion = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        DateTime fechaExpiracion = DateTime.Now.AddMinutes(30);
        _usuarioMPP.CrearSesionValidacionRegistro(usuario, _seguridad.GenerarHashToken(tokenGestion), fechaExpiracion);
        return new ResultadoRegistroAgenciaValidacion(tokenGestion, fechaExpiracion);
    }

    private Usuario ObtenerUsuarioPendientePorTokenGestion(string tokenGestion)
    {
        if (string.IsNullOrWhiteSpace(tokenGestion)) throw new UnauthorizedAccessException("La sesión de validación no es válida o venció.");
        int? idUsuario = _usuarioMPP.ConsultarUsuarioSesionValidacionRegistro(_seguridad.GenerarHashToken(tokenGestion));
        Usuario? usuario = idUsuario.HasValue ? _usuarioMPP.ConsultarUsuarioPorId(new Usuario { ID = idUsuario.Value }) : null;
        if (usuario is null || !string.Equals(usuario.Estado, "PendienteValidacion", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("La sesión de validación no es válida o venció.");
        return usuario;
    }

    private static void ValidarDatosRegistro(Usuario usuario, ContratacionServicio contratacionServicio)
    {
        if (string.IsNullOrWhiteSpace(contratacionServicio.ReferenciaContratacion) || string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellido) || string.IsNullOrWhiteSpace(usuario.Email) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Completá todos los datos obligatorios del registro.");
        }

        if (!MailAddress.TryCreate(usuario.Email.Trim(), out _))
        {
            throw new ArgumentException("Ingresá un email laboral válido.");
        }

        if (usuario.PasswordHash.Length < 8 || !usuario.PasswordHash.Any(char.IsLetter) || !usuario.PasswordHash.Any(char.IsDigit))
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres e incluir letras y números.");
        }
    }

    private Usuario ValidarUsuarioConAgencia(Usuario usuario) { 
        if (!usuario.IdAgencia.HasValue) { 
            throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); 
        } 
        return usuario; 
    }
    private void ValidarPermiso(Usuario usuario, string permiso) { 
        if (!_rolBLL.TienePermiso(usuario, permiso)) { 
            throw new UnauthorizedAccessException("No tenés permiso para realizar esta acción."); 
        } 
    }
}
