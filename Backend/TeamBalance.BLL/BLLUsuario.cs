using System.Net.Mail;
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
    private readonly BLLRol _rolBLL;

    public BLLUsuario(MPPUsuario usuarioMPP, BLLBitacora bitacoraBLL, Seguridad seguridad, EmailService emailService, RecaptchaService recaptchaService, BLLRol rolBLL)
    {
        _usuarioMPP = usuarioMPP;
        _bitacoraBLL = bitacoraBLL;
        _seguridad = seguridad;
        _emailService = emailService;
        _recaptchaService = recaptchaService;
        _rolBLL = rolBLL;
    }

    public bool EmailDisponible(string email)
    {
        return !_usuarioMPP.ExisteUsuarioPorEmail(email);
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(string email)
    {
        return _usuarioMPP.ConsultarUsuarioPendienteValidacion(email);
    }

    public void PrepararUsuarioDueño(Usuario usuario, Rol rol)
    {
        string password = usuario.PasswordHash;

        usuario.Rol = rol;
        List<Rol> roles = new List<Rol>();
        roles.Add(rol);
        usuario.Roles = roles;
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
        Dueño dueño = new Dueño(true);
        return dueño;
    }

    public ValidacionCuentum CrearValidacionEmail(out string token)
    {
        token = Guid.NewGuid().ToString("N");

        DateTime fechaGeneracion = DateTime.Now;
        DateTime fechaExpiracion = fechaGeneracion.AddHours(24);
        ValidacionCuentum validacion = new ValidacionCuentum(0, 0, "Email", _seguridad.GenerarHashToken(token), fechaGeneracion, fechaExpiracion, false, null, true);
        return validacion;
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

    public async Task<InicioSesionResultado> IniciarSesion(Usuario usuarioEntrante, bool mantenerSesion)
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

        Bitacora bitacora;

        if (usuarioBD is null || !_seguridad.VerificarPassword(usuarioEntrante.PasswordHash, usuarioBD.PasswordHash))
        {
            bitacora = new Bitacora(null,null,"Usuario",null, "IniciarSesion", "Se rechazó un intento de inicio de sesión por credenciales inválidas.", "Denegado", "Advertencia","Seguridad");
            _bitacoraBLL.Add(bitacora);

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

        _rolBLL.CargarAutorizacion(usuarioBD);

        string accessToken = _seguridad.GenerarTokenSeguro();
        DateTime fechaExpiracion = mantenerSesion
            ? DateTime.Now.AddDays(DuracionSesionRecordadaDias)
            : DateTime.Now.AddHours(DuracionSesionNormalHoras);

        DateTime fechaInicio = DateTime.Now;
        SesionUsuario sesion = new SesionUsuario(0, usuarioBD.ID, _seguridad.GenerarHashToken(accessToken), fechaInicio, fechaInicio, fechaExpiracion, null, true, null);

        _usuarioMPP.RegistrarSesion(sesion);

        bitacora = new Bitacora(usuarioBD.ID, usuarioBD.IdAgencia, "Usuario",usuarioBD.ID, "IniciarSesion", "El usuario inició sesión en TeamBalance.","Exitoso","Informacion","Seguridad");
        _bitacoraBLL.Add(bitacora);

        InicioSesionResultado resultado = new InicioSesionResultado(usuarioBD, accessToken, fechaExpiracion);
        return resultado;
    }

    public bool SesionVigente(string accessToken)
    {
        return !string.IsNullOrWhiteSpace(accessToken) && _usuarioMPP.SesionVigente(_seguridad.GenerarHashToken(accessToken));
    }

    public Usuario? ConsultarUsuarioSesion(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        SesionUsuario sesion = new SesionUsuario(0, 0, _seguridad.GenerarHashToken(accessToken), DateTime.MinValue, null, DateTime.MinValue, null, false, null);

        Usuario? usuario = _usuarioMPP.ConsultarUsuarioPorSesion(sesion);
        if (usuario is not null)
        {
            _rolBLL.CargarAutorizacion(usuario);
        }
        return usuario;
    }

    public void CerrarSesion(string accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _usuarioMPP.CerrarSesion(_seguridad.GenerarHashToken(accessToken));
        }
    }

    public List<Usuario> ConsultarUsuariosAgencia(Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarUsuarios");
        if (solicitante.IdAgencia is null)
        {
            throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.");
        }

        List<Usuario> usuarios = _usuarioMPP.ConsultarUsuariosAgencia(solicitante);
        foreach (Usuario usuario in usuarios)
        {
            _rolBLL.CargarAutorizacion(usuario);
        }
        return usuarios;
    }

    public bool SoporteInicialDisponible(Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarUsuarios");
        return !_usuarioMPP.ExisteSoporte();
    }

    public async Task<Usuario> RegistrarUsuarioAgencia(Usuario usuario, Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarUsuarios");
        PrepararUsuarioGestion(usuario, solicitante);

        bool esSoporte = usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase));
        if (esSoporte)
        {
            if (usuario.Roles.Count != 1)
            {
                throw new ArgumentException("El usuario inicial de Soporte sólo puede tener el rol Soporte.");
            }
            if (_usuarioMPP.ExisteSoporte())
            {
                throw new InvalidOperationException("El usuario inicial de Soporte ya fue creado. La gestión posterior se realiza desde Soporte.");
            }
            usuario.IdAgencia = null;
        }
        else
        {
            usuario.IdAgencia = solicitante.IdAgencia;
        }

        usuario.ID = _usuarioMPP.RegistrarUsuario(usuario);
        _usuarioMPP.ReemplazarRoles(usuario);
        RegistrarPerfiles(usuario);
        _rolBLL.CargarAutorizacion(usuario);

        ValidacionCuentum validacion = CrearValidacionEmail(out string token);
        _usuarioMPP.ReemplazarValidacionEmail(usuario, validacion);
        bool correoEnviado = await _emailService.EnviarCorreoValidacion(usuario.Email, usuario.Nombre, token);

        Bitacora bitacora = new Bitacora(0, solicitante.ID, solicitante.IdAgencia, "Usuario", usuario.ID, esSoporte ? "RegistrarSoporteInicial" : "RegistrarUsuarioAgencia", esSoporte ? "Se creó el usuario inicial de Soporte de TeamBalance." : "Se registró un usuario para la agencia.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia", "Usuarios", DateTime.Now, null);
        _bitacoraBLL.Add(bitacora);

        return usuario;
    }

    public void ModificarUsuarioAgencia(Usuario usuario, Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarUsuarios");
        if (!_usuarioMPP.ConsultarUsuariosAgencia(solicitante).Any(item => item.ID == usuario.ID))
        {
            throw new KeyNotFoundException("No existe el usuario dentro de la agencia.");
        }
        List<int> rolesSolicitados = usuario.Roles.Select(rol => rol.ID).Distinct().ToList();
        usuario.Roles = _rolBLL.ConsultarRolesAsignablesAgencia(solicitante).Where(rol => rolesSolicitados.Contains(rol.ID)).ToList();
        if (usuario.Roles.Count != rolesSolicitados.Count)
        {
            throw new ArgumentException("Uno o más roles seleccionados no están disponibles.");
        }
        ValidarUsuarioGestion(usuario, false);
        Usuario? existente = _usuarioMPP.ConsultarUsuarioPorEmail(usuario.Email.Trim().ToLowerInvariant());
        if (existente is not null && existente.ID != usuario.ID)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese email.");
        }
        usuario.IdAgencia = solicitante.IdAgencia;
        usuario.Rol = usuario.Roles.First();
        _usuarioMPP.ModificarUsuario(usuario);
        _usuarioMPP.ReemplazarRoles(usuario);
        RegistrarPerfiles(usuario);
    }

    public void CambiarEstadoUsuarioAgencia(int idUsuario, bool activo, Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarUsuarios");
        Usuario usuario = _usuarioMPP.ConsultarUsuariosAgencia(solicitante).FirstOrDefault(item => item.ID == idUsuario) ?? throw new KeyNotFoundException("No existe el usuario dentro de la agencia.");
        _usuarioMPP.CambiarEstado(usuario, activo);
        _bitacoraBLL.Add(new Bitacora() { IdUsuario = solicitante.ID, IdAgencia = solicitante.IdAgencia, Entidad = "Usuario", IdEntidad = idUsuario, Accion = activo ? "ActivarUsuario" : "DarBajaUsuario", Mensaje = activo ? "Se activó un usuario de la agencia." : "Se dio de baja un usuario de la agencia.", Resultado = "Exitoso", Criticidad = "Informacion", Modulo = "Usuarios", FechaHora = DateTime.Now });
    }

    private void PrepararUsuarioGestion(Usuario usuario, Usuario solicitante)
    {
        if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios"))
        {
            throw new UnauthorizedAccessException("No tenés permiso para gestionar usuarios.");
        }
        List<int> rolesSolicitados = usuario.Roles.Select(rol => rol.ID).Distinct().ToList();
        List<Rol> rolesActivos = _rolBLL.ConsultarRolesAsignablesAgencia(solicitante);
        Rol rolSoporte = _rolBLL.ConsultarRolPorNombre("Soporte");
        if (!_usuarioMPP.ExisteSoporte() && rolesSolicitados.Contains(rolSoporte.ID))
        {
            rolesActivos.Add(rolSoporte);
        }
        usuario.Roles = rolesActivos.Where(rol => rolesSolicitados.Contains(rol.ID)).ToList();
        if (usuario.Roles.Count != rolesSolicitados.Count)
        {
            throw new ArgumentException("Uno o más roles seleccionados no están disponibles.");
        }
        ValidarUsuarioGestion(usuario, true);
        usuario.Nombre = usuario.Nombre.Trim();
        usuario.Apellido = usuario.Apellido.Trim();
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        usuario.PasswordHash = _seguridad.GenerarHashPassword(usuario.PasswordHash);
        usuario.Estado = "PendienteValidacion";
        usuario.FechaAlta = DateTime.Now;
        usuario.Activo = true;
        usuario.Rol = usuario.Roles.First();
    }

    private static void ValidarUsuarioGestion(Usuario usuario, bool passwordObligatoria)
    {
        if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellido) || string.IsNullOrWhiteSpace(usuario.Email) || usuario.Roles.Count == 0)
        {
            throw new ArgumentException("Completá nombre, apellido, email y al menos un rol.");
        }
        if (!MailAddress.TryCreate(usuario.Email.Trim(), out _))
        {
            throw new ArgumentException("Ingresá un email válido.");
        }
        if (passwordObligatoria && string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Ingresá una contraseña temporal.");
        }
        if (usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase)) && usuario.Empleado is null)
        {
            throw new ArgumentException("Completá los datos laborales del empleado.");
        }
    }

    private void RegistrarPerfiles(Usuario usuario)
    {
        if (usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase)))
        {
            _usuarioMPP.RegistrarEmpleado(usuario);
        }
        if (usuario.Roles.Any(rol => string.Equals(rol.TipoUsuario, "PM", StringComparison.OrdinalIgnoreCase)))
        {
            _usuarioMPP.RegistrarPM(usuario);
        }
    }

    private void ValidarPermiso(Usuario usuario, string codigoPermiso)
    {
        if (!_rolBLL.TienePermiso(usuario, codigoPermiso))
        {
            throw new UnauthorizedAccessException("No tenés permiso para realizar esta operación.");
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
        DateTime fechaGeneracion = DateTime.Now;
        ValidacionCuentum validacion = new ValidacionCuentum(0, usuarioBD.ID, "RecuperacionPassword", _seguridad.GenerarHashToken(token), fechaGeneracion, fechaGeneracion.AddMinutes(30), false, null, true);

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

        ValidacionCuentum validacion = new ValidacionCuentum(0, 0, "RecuperacionPassword", _seguridad.GenerarHashToken(token), DateTime.MinValue, DateTime.MinValue, false, null, false);

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

        SesionUsuario sesion = new SesionUsuario(0, 0, _seguridad.GenerarHashToken(accessToken), DateTime.MinValue, null, DateTime.MinValue, null, false, null);

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
        Bitacora bitacora = new Bitacora(0, usuario?.ID, usuario?.IdAgencia, "Usuario", usuario?.ID, accion, mensaje, resultado, criticidad, "Seguridad", DateTime.Now, null);

        _bitacoraBLL.Add(bitacora);
    }

}
