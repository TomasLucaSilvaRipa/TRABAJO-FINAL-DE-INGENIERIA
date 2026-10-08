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
    private readonly MPPRecursos _recursosMPP;
    private readonly MPPTarea _tareaMPP;

    public BLLUsuario(MPPUsuario usuarioMPP, BLLBitacora bitacoraBLL, Seguridad seguridad, EmailService emailService, RecaptchaService recaptchaService, BLLRol rolBLL, MPPRecursos recursosMPP, MPPTarea tareaMPP)
    {
        _usuarioMPP = usuarioMPP;
        _bitacoraBLL = bitacoraBLL;
        _seguridad = seguridad;
        _emailService = emailService;
        _recaptchaService = recaptchaService;
        _rolBLL = rolBLL;
        _recursosMPP = recursosMPP;
        _tareaMPP = tareaMPP;
    }

    public bool EmailDisponible(Usuario usuario)
    {
        return !_usuarioMPP.ExisteUsuarioPorEmail(usuario);
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(Usuario usuario)
    {
        return _usuarioMPP.ConsultarUsuarioPendienteValidacion(usuario);
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

    public ValidacionCuenta CrearValidacionEmail(out string token)
    {
        token = Guid.NewGuid().ToString("N");

        DateTime fechaGeneracion = DateTime.Now;
        DateTime fechaExpiracion = fechaGeneracion.AddHours(24);
        ValidacionCuenta validacion = new ValidacionCuenta(0, 0, "Email", _seguridad.GenerarHashToken(token), fechaGeneracion, fechaExpiracion, false, null, true);
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

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuenta validacion)
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
            await _recaptchaService.ValidarLogin(usuarioEntrante);
        }
        catch (UnauthorizedAccessException ex){ RegistrarEventoSeguridad(null, "IniciarSesion", "Se rechazó un intento de inicio de sesión por una verificación reCAPTCHA inválida.", "Denegado", "Advertencia"); throw new UnauthorizedAccessException("No fue posible validar la verificación de seguridad.", ex); }

        Usuario? usuario = _usuarioMPP.ConsultarUsuarioPorEmail(usuarioEntrante);

        Bitacora bitacora;

        if (usuario is null || !_seguridad.VerificarPassword(usuarioEntrante.PasswordHash, usuario.PasswordHash))
        {
            bitacora = new Bitacora(null,null,"Usuario",null, "IniciarSesion", "Se rechazó un intento de inicio de sesión por credenciales inválidas.", "Denegado", "Advertencia","Seguridad");
            _bitacoraBLL.Add(bitacora);

            throw new UnauthorizedAccessException("El email o la contraseña no son correctos.");
        }

        if (!usuario.Activo)
        {
            throw new InvalidOperationException("La cuenta se encuentra inactiva.");
        }

        if (!string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Confirmá tu correo electrónico antes de iniciar sesión.");
        }

        _rolBLL.CargarAutorizacion(usuario);

        string accessToken = _seguridad.GenerarTokenSeguro();
        DateTime fechaExpiracion = mantenerSesion ? DateTime.Now.AddDays(DuracionSesionRecordadaDias) : DateTime.Now.AddHours(DuracionSesionNormalHoras);

        DateTime fechaInicio = DateTime.Now;
        SesionUsuario sesion = new SesionUsuario(0, usuario.ID, _seguridad.GenerarHashToken(accessToken), fechaInicio, fechaInicio, fechaExpiracion, null, true, null);

        _usuarioMPP.RegistrarSesion(sesion);

        bitacora = new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario",usuario.ID, "IniciarSesion", "El usuario inició sesión en TeamBalance.","Exitoso","Informacion","Seguridad");
        _bitacoraBLL.Add(bitacora);

        InicioSesionResultado resultado = new InicioSesionResultado(usuario, accessToken, fechaExpiracion);
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

    public List<Usuario> ConsultarOperadores(Usuario solicitante)
    {
        ExigirGestionOperadores(solicitante);
        List<Usuario> operadores = _usuarioMPP.ConsultarOperadores();
        foreach (Usuario operador in operadores)
        {
            _rolBLL.CargarAutorizacion(operador);
        }
        return operadores;
    }

    public async Task<Usuario> RegistrarOperador(Usuario operador, Usuario solicitante)
    {
        ExigirGestionOperadores(solicitante);
        PrepararOperador(operador, true);
        operador.ID = _usuarioMPP.RegistrarUsuario(operador);
        _usuarioMPP.ReemplazarRoles(operador);
        _rolBLL.CargarAutorizacion(operador);

        ValidacionCuenta validacion = CrearValidacionEmail(out string token);
        _usuarioMPP.ReemplazarValidacionEmail(operador, validacion);
        bool correoEnviado = await _emailService.EnviarCorreoValidacion(operador, token);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, null, "Usuario", operador.ID, "RegistrarOperador", "Se creó un operador interno de TeamBalance.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia", "Operadores"));
        return operador;
    }

    public void ModificarOperador(Usuario operador, Usuario solicitante)
    {
        ExigirGestionOperadores(solicitante);
        Usuario existente = _usuarioMPP.ConsultarOperadores().FirstOrDefault(item => item.ID == operador.ID) ?? throw new KeyNotFoundException("No existe el operador indicado.");
        PrepararOperador(operador, false);
        Usuario? mismoEmail = _usuarioMPP.ConsultarUsuarioPorEmail(operador);
        if (mismoEmail is not null && mismoEmail.ID != operador.ID)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese email.");
        }
        operador.Activo = existente.Activo;
        operador.Estado = existente.Estado;
        _usuarioMPP.ModificarUsuario(operador);
        _usuarioMPP.ReemplazarRoles(operador);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, null, "Usuario", operador.ID, "ModificarOperador", "Se modificó un operador interno de TeamBalance.", "Exitoso", "Informacion", "Operadores"));
    }

    public void CambiarEstadoOperador(Usuario operador, Usuario solicitante)
    {
        ExigirGestionOperadores(solicitante);
        Usuario existente = _usuarioMPP.ConsultarOperadores().FirstOrDefault(item => item.ID == operador.ID) ?? throw new KeyNotFoundException("No existe el operador indicado.");
        if (!operador.Activo && existente.ID == solicitante.ID)
        {
            throw new InvalidOperationException("No podés dar de baja tu propio acceso de Soporte.");
        }
        existente.Activo = operador.Activo;
        _usuarioMPP.CambiarEstado(existente);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, null, "Usuario", existente.ID, existente.Activo ? "ActivarOperador" : "DarBajaOperador", existente.Activo ? "Se activó un operador interno." : "Se dio de baja un operador interno.", "Exitoso", "Informacion", "Operadores"));
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

        ValidacionCuenta validacion = CrearValidacionEmail(out string token);
        _usuarioMPP.ReemplazarValidacionEmail(usuario, validacion);
        bool correoEnviado = await _emailService.EnviarCorreoValidacion(usuario, token);

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
        Usuario? existente = _usuarioMPP.ConsultarUsuarioPorEmail(usuario);
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

    public void CambiarEstadoUsuarioAgencia(Usuario usuario, Usuario solicitante)
    {
        try
        {
            ValidarPermiso(solicitante, "GestionarUsuarios");
            Usuario usuarioAgencia = _usuarioMPP.ConsultarUsuariosAgencia(solicitante).FirstOrDefault(item => item.ID == usuario.ID) ?? throw new KeyNotFoundException("No existe el usuario dentro de la agencia.");
            if (!usuario.Activo && usuarioAgencia.Empleado is not null)
            {
                int idEmpleado = _recursosMPP.ConsultarIdEmpleadoPorUsuarioAgencia(usuarioAgencia, solicitante);
                List<Tarea> tareasPendientes = _tareaMPP.Consultar(solicitante, new FiltroTarea { IdEmpleadoAsignado = idEmpleado })
                    .Where(tarea => tarea.Activo && !string.Equals(tarea.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase) && !string.Equals(tarea.Estado, "Finalizado", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (tareasPendientes.Count > 0)
                {
                    string detalle = string.Join(", ", tareasPendientes.Take(3).Select(tarea => $"{tarea.Titulo}{(tarea.Deadline.HasValue ? $" (vence {tarea.Deadline.Value:dd/MM/yyyy})" : string.Empty)}"));
                    string adicionales = tareasPendientes.Count > 3 ? $" y {tareasPendientes.Count - 3} más" : string.Empty;
                    throw new InvalidOperationException($"No se puede dar de baja al empleado porque tiene {tareasPendientes.Count} tarea(s) activa(s): {detalle}{adicionales}. Reasignalas desde Planificar y asignar tareas antes de continuar.");
                }
            }
            usuarioAgencia.Activo = usuario.Activo;
            _usuarioMPP.CambiarEstado(usuarioAgencia);
            Bitacora bitacora = new Bitacora(solicitante.ID, solicitante.IdAgencia, "Usuario", usuarioAgencia.ID, usuarioAgencia.Activo ? "ActivarUsuario" : "DarBajaUsuario", usuarioAgencia.Activo ? "Se activó un usuario de la agencia." : "Se dio de baja un usuario de la agencia.", "Exitoso", "Informacion", "Usuarios");
            _bitacoraBLL.Add(bitacora);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (InvalidOperationException) { throw; }
        catch(Exception ex) { throw new Exception(ex.Message); }
    }

    private void PrepararUsuarioGestion(Usuario usuario, Usuario solicitante)
    {
        if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios"))
        {
            throw new UnauthorizedAccessException("No tenés permiso para gestionar usuarios.");
        }
        List<int> rolesSolicitados = usuario.Roles.Select(rol => rol.ID).Distinct().ToList();
        List<Rol> rolesActivos = _rolBLL.ConsultarRolesAsignablesAgencia(solicitante);
        Rol rol = new Rol("Soporte");
        Rol rolSoporte = _rolBLL.ConsultarRolPorNombre(rol);
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
        if (_usuarioMPP.ConsultarUsuarioPorEmail(usuario) is not null)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese email.");
        }
        usuario.PasswordHash = _seguridad.GenerarHashPassword(usuario.PasswordHash);
        usuario.Estado = "PendienteValidacion";
        usuario.FechaAlta = DateTime.Now;
        usuario.Activo = true;
        usuario.AceptaTerminos = true;
        usuario.Rol = usuario.Roles.First();
    }

    private void PrepararOperador(Usuario operador, bool passwordObligatoria)
    {
        List<int> rolesSolicitados = operador.Roles.Select(rol => rol.ID).Distinct().ToList();
        List<Rol> rolesSoporte = _rolBLL.ConsultarRolesActivos()
            .Where(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase))
            .ToList();
        operador.Roles = rolesSoporte.Where(rol => rolesSolicitados.Contains(rol.ID)).ToList();
        if (operador.Roles.Count != rolesSolicitados.Count || operador.Roles.Count == 0)
        {
            throw new ArgumentException("Seleccioná al menos un rol interno de Soporte válido.");
        }
        ValidarUsuarioGestion(operador, passwordObligatoria);
        operador.Nombre = operador.Nombre.Trim();
        operador.Apellido = operador.Apellido.Trim();
        operador.Email = operador.Email.Trim().ToLowerInvariant();
        operador.IdAgencia = null;
        operador.Rol = operador.Roles.First();
        if (passwordObligatoria)
        {
            if (_usuarioMPP.ConsultarUsuarioPorEmail(operador) is not null)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese email.");
            }
            operador.PasswordHash = _seguridad.GenerarHashPassword(operador.PasswordHash);
            operador.Estado = "PendienteValidacion";
            operador.FechaAlta = DateTime.Now;
            operador.Activo = true;
            operador.AceptaTerminos = true;
        }
    }

    private void ExigirGestionOperadores(Usuario solicitante)
    {
        ValidarPermiso(solicitante, "GestionarOperadores");
        if (!solicitante.Roles.Any(rol => string.Equals(rol.TipoUsuario, "Soporte", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Sólo Soporte TeamBalance puede gestionar operadores internos.");
        }
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
        try
        {
            if (!string.IsNullOrWhiteSpace(usuario.Email))
            {
                usuario = _usuarioMPP.ConsultarUsuarioPorEmail(usuario);

                if (usuario is null || !usuario.Activo || !string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string token = _seguridad.GenerarTokenRecuperacion();
                DateTime fechaGeneracion = DateTime.Now;
                ValidacionCuenta validacion = new ValidacionCuenta(0, usuario.ID, "RecuperacionPassword", _seguridad.GenerarHashToken(token), fechaGeneracion, fechaGeneracion.AddMinutes(30), false, null, true);

                _usuarioMPP.ReemplazarRecuperacionPassword(usuario, validacion);

                bool correoEnviado = await _emailService.EnviarCorreoRecuperoPassword(usuario.Email, usuario.Nombre, token);

                if (!correoEnviado){ throw new InvalidOperationException("No fue posible enviar el correo de recuperación."); }

                RegistrarEventoSeguridad(usuario, "SolicitarRecuperoPassword", "Se generó un enlace temporal para recuperar la contraseña.", "Exitoso", "Informacion");
            }
            else { throw new ArgumentException("Ingresá un email válido.");  }  
        }
        catch(Exception ex) { throw new Exception(ex.Message); }
    }

    public async Task RestablecerPassword(Usuario usuario, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Ingresá una nueva contraseña y utilizá un enlace válido.");
        }

        _seguridad.ValidarPassword(usuario.PasswordHash);

        ValidacionCuenta validacion = new ValidacionCuenta(0, 0, "RecuperacionPassword", _seguridad.GenerarHashToken(token), DateTime.MinValue, DateTime.MinValue, false, null, false);

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
