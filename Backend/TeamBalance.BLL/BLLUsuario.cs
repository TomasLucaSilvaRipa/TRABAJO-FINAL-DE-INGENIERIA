using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLUsuario
{
    private const int DuracionSesionNormalHoras = 8;
    private const int DuracionSesionRecordadaDias = 30;
    private readonly MPPUsuario _usuarioMPP;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly Seguridad _seguridad;
    private readonly EmailService _emailService;
    private readonly RecaptchaService _recaptchaService;

    public BLLUsuario(MPPUsuario usuarioMPP, BLLBitacora bitacoraBLL, Seguridad seguridad, EmailService emailService, RecaptchaService recaptchaService)
    {
        _usuarioMPP = usuarioMPP;
        _bitacoraBLL = bitacoraBLL;
        _seguridad = seguridad;
        _emailService = emailService;
        _recaptchaService = recaptchaService;
    }

    public bool EmailDisponible(string email)
    {
        return !_usuarioMPP.ExisteUsuarioPorEmail(email);
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(string email)
    {
        return _usuarioMPP.ConsultarUsuarioPendienteValidacion(email);
    }

    //pasar paramtro objeto
    public void PrepararUsuarioDueño(Usuario usuario, int idRol)
    {
        string password = usuario.PasswordHash;

        usuario.IdRol = idRol;
        usuario.Nombre = usuario.Nombre.Trim();
        usuario.Apellido = usuario.Apellido.Trim();
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        usuario.PasswordHash = _seguridad.GenerarHashPassword(password);
        usuario.Estado = "PendienteValidacion";
        usuario.FechaAlta = DateTime.Now;
        usuario.Activo = true;
    }

    public Dueño CrearDueño()
    {
        return new Dueño
        {
            Activo = true,
        };
    }

    public ValidacionCuentum CrearValidacionEmail(out string token)
    {
        token = Guid.NewGuid().ToString("N");

        return new ValidacionCuentum
        {
            Metodo = "Email",
            TokenHash = _seguridad.GenerarHashToken(token),
            FechaGeneracion = DateTime.Now,
            FechaExpiracion = DateTime.Now.AddHours(24),
            Utilizado = false,
            Activo = true,
        };
    }

    public bool ValidarCuenta(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return _usuarioMPP.ValidarCuenta(_seguridad.GenerarHashToken(token));
    }

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuentum validacion)
    {
        _usuarioMPP.ReemplazarValidacionEmail(usuario, validacion);
    }

    public async Task<(Usuario Usuario, string AccessToken, DateTime FechaExpiracion)> IniciarSesion(Usuario usuarioEntrante, bool mantenerSesion)
    {
        if (string.IsNullOrWhiteSpace(usuarioEntrante.Email) || string.IsNullOrWhiteSpace(usuarioEntrante.PasswordHash) || string.IsNullOrWhiteSpace(usuarioEntrante.RecaptchaToken))
        {
            throw new ArgumentException("Ingresá tu email y contraseña.");
        }

        try
        {
            await _recaptchaService.ValidarLogin(usuarioEntrante.RecaptchaToken);
        }
        catch (UnauthorizedAccessException ex){ RegistrarEventoSeguridad(null, "IniciarSesion", "Se rechazó un intento de inicio de sesión por una verificación reCAPTCHA inválida.", "Denegado", "Advertencia"); throw new UnauthorizedAccessException("No fue posible validar la verificación de seguridad.", ex); }

        Usuario? usuarioBD = _usuarioMPP.ConsultarUsuarioPorEmail(usuarioEntrante.Email.Trim().ToLowerInvariant());

        if (usuarioBD is null || !_seguridad.VerificarPassword(usuarioEntrante.PasswordHash, usuarioBD.PasswordHash))
        {
            _bitacoraBLL.Add(new Bitacora()
            {
                Entidad = "Usuario",
                Accion = "IniciarSesion",
                Mensaje = "Se rechazó un intento de inicio de sesión por credenciales inválidas.",
                Resultado = "Denegado",
                Criticidad = "Advertencia",
                Modulo = "Seguridad",
                FechaHora = DateTime.Now,
            });

            throw new UnauthorizedAccessException("El email o la contraseña no son correctos.");
        }

        if (!usuarioBD.Activo)
        {
            throw new InvalidOperationException("La cuenta se encuentra inactiva.");
        }

        if (!string.Equals(usuarioBD.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Confirmá tu correo electrónico antes de iniciar sesión.");
        }

        string accessToken = _seguridad.GenerarTokenSeguro();
        DateTime fechaExpiracion = mantenerSesion
            ? DateTime.Now.AddDays(DuracionSesionRecordadaDias)
            : DateTime.Now.AddHours(DuracionSesionNormalHoras);

        SesionUsuario sesion = new SesionUsuario
        {
            IdUsuario = usuarioBD.ID,
            TokenHash = _seguridad.GenerarHashToken(accessToken),
            FechaInicio = DateTime.Now,
            FechaUltimaActividad = DateTime.Now,
            FechaExpiracion = fechaExpiracion,
            Activa = true,
        };

        _usuarioMPP.RegistrarSesion(sesion);

        _bitacoraBLL.Add(new Bitacora()
        {
            IdUsuario = usuarioBD.ID,
            IdAgencia = usuarioBD.IdAgencia,
            Entidad = "Usuario",
            IdEntidad = usuarioBD.ID,
            Accion = "IniciarSesion",
            Mensaje = "El usuario inició sesión en TeamBalance.",
            Resultado = "Exitoso",
            Criticidad = "Informacion",
            Modulo = "Seguridad",
            FechaHora = DateTime.Now,
        });

        return (usuarioBD, accessToken, fechaExpiracion);
    }

    public bool SesionVigente(string accessToken)
    {
        return !string.IsNullOrWhiteSpace(accessToken) && _usuarioMPP.SesionVigente(_seguridad.GenerarHashToken(accessToken));
    }

    public void CerrarSesion(string accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _usuarioMPP.CerrarSesion(_seguridad.GenerarHashToken(accessToken));
        }
    }

    public async Task SolicitarRecuperoPassword(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario.Email))
        {
            throw new ArgumentException("Ingresá un email válido.");
        }

        Usuario? usuarioBD = _usuarioMPP.ConsultarUsuarioPorEmail(usuario.Email.Trim().ToLowerInvariant());

        if (usuarioBD is null || !usuarioBD.Activo || !string.Equals(usuarioBD.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string token = _seguridad.GenerarTokenRecuperacion();
        ValidacionCuentum validacion = new ValidacionCuentum()
        {
            IdUsuario = usuarioBD.ID,
            Metodo = "RecuperacionPassword",
            TokenHash = _seguridad.GenerarHashToken(token),
            FechaGeneracion = DateTime.Now,
            FechaExpiracion = DateTime.Now.AddMinutes(30),
            Utilizado = false,
            Activo = true,
        };

        _usuarioMPP.ReemplazarRecuperacionPassword(usuarioBD, validacion);

        bool correoEnviado = await _emailService.EnviarCorreoRecuperoPassword(usuarioBD.Email, usuarioBD.Nombre, token);

        if (!correoEnviado)
        {
            throw new InvalidOperationException("No fue posible enviar el correo de recuperación.");
        }

        RegistrarEventoSeguridad(usuarioBD, "SolicitarRecuperoPassword", "Se generó un enlace temporal para recuperar la contraseña.", "Exitoso", "Informacion");
    }

    public async Task RestablecerPassword(Usuario usuario, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Ingresá una nueva contraseña y utilizá un enlace válido.");
        }

        _seguridad.ValidarPassword(usuario.PasswordHash);

        ValidacionCuentum validacion = new ValidacionCuentum()
        {
            Metodo = "RecuperacionPassword",
            TokenHash = _seguridad.GenerarHashToken(token),
        };

        Usuario? usuarioBD = _usuarioMPP.ConsultarUsuarioPorRecuperacionPassword(validacion);

        if (usuarioBD is null)
        {
            throw new ArgumentException("El enlace de recuperación es inválido, venció o ya fue utilizado.");
        }

        usuarioBD.PasswordHash = _seguridad.GenerarHashPassword(usuario.PasswordHash);

        if (!_usuarioMPP.RestablecerPassword(usuarioBD, validacion))
        {
            throw new InvalidOperationException("No fue posible restablecer la contraseña.");
        }

        bool correoEnviado = await _emailService.EnviarCorreoPasswordModificada(usuarioBD.Email, usuarioBD.Nombre);
        RegistrarEventoSeguridad(usuarioBD, "RestablecerPassword", correoEnviado ? "La contraseña fue restablecida mediante un enlace temporal." : "La contraseña fue restablecida, pero no se pudo enviar el correo de confirmación.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia");
    }

    public async Task CambiarPassword(Usuario usuario, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
        }

        if (string.IsNullOrWhiteSpace(usuario.PasswordActual) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Completá la contraseña actual y la nueva contraseña.");
        }

        SesionUsuario sesion = new SesionUsuario()
        {
            TokenHash = _seguridad.GenerarHashToken(accessToken),
        };

        Usuario? usuarioBD = _usuarioMPP.ConsultarUsuarioPorSesion(sesion);

        if (usuarioBD is null)
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
        }

        if (!_seguridad.VerificarPassword(usuario.PasswordActual, usuarioBD.PasswordHash))
        {
            RegistrarEventoSeguridad(usuarioBD, "CambiarPassword", "Se rechazó un cambio de contraseña porque la contraseña actual no coincidió.", "Denegado", "Advertencia");
            throw new UnauthorizedAccessException("La contraseña actual no es correcta.");
        }

        _seguridad.ValidarPassword(usuario.PasswordHash);
        usuarioBD.PasswordHash = _seguridad.GenerarHashPassword(usuario.PasswordHash);

        if (!_usuarioMPP.CambiarPassword(usuarioBD))
        {
            throw new InvalidOperationException("No fue posible modificar la contraseña.");
        }

        bool correoEnviado = await _emailService.EnviarCorreoPasswordModificada(usuarioBD.Email, usuarioBD.Nombre);
        RegistrarEventoSeguridad(usuarioBD, "CambiarPassword", correoEnviado ? "El usuario modificó su contraseña desde una sesión autenticada." : "El usuario modificó su contraseña, pero no se pudo enviar el correo de confirmación.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia");
    }

    private void RegistrarEventoSeguridad(Usuario? usuario, string accion, string mensaje, string resultado, string criticidad)
    {
        Bitacora bitacora = new Bitacora()
        {
            IdUsuario = usuario?.ID,
            IdAgencia = usuario?.IdAgencia,
            Entidad = "Usuario",
            IdEntidad = usuario?.ID,
            Accion = accion,
            Mensaje = mensaje,
            Resultado = resultado,
            Criticidad = criticidad,
            Modulo = "Seguridad",
            FechaHora = DateTime.Now,
        };

        _bitacoraBLL.Add(bitacora);
    }

}
