using System.Net.Mail;
using OtpNet;
using System.Text.RegularExpressions;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLUsuario
{
    private const int TamanoMaximoFotoPerfilBytes = 512 * 1024;
    private static readonly Regex FotoPerfilDataUrlRegex = new("^data:(image/(?:png|jpeg|webp));base64,([A-Za-z0-9+/]+={0,2})$", RegexOptions.CultureInvariant);
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
    private readonly FieldEncryptionService _fieldEncryptionService;
    private readonly BLLPasswordSecurity _passwordSecurityBLL;

    public BLLUsuario(MPPUsuario usuarioMPP, BLLBitacora bitacoraBLL, Seguridad seguridad, EmailService emailService, RecaptchaService recaptchaService, BLLRol rolBLL, MPPRecursos recursosMPP, MPPTarea tareaMPP, FieldEncryptionService fieldEncryptionService, BLLPasswordSecurity passwordSecurityBLL)
    {
        _usuarioMPP = usuarioMPP;
        _bitacoraBLL = bitacoraBLL;
        _seguridad = seguridad;
        _emailService = emailService;
        _recaptchaService = recaptchaService;
        _rolBLL = rolBLL;
        _recursosMPP = recursosMPP;
        _tareaMPP = tareaMPP;
        _fieldEncryptionService = fieldEncryptionService;
        _passwordSecurityBLL = passwordSecurityBLL;
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

        bool resultado = _usuarioMPP.ValidarCuenta(_seguridad.GenerarHashToken(token));
        if (resultado)
        {
            _bitacoraBLL.Add(new Bitacora(null, null, "ValidacionCuenta", null, "ValidarCuenta", "Se validó una cuenta mediante el enlace de correo electrónico.", "Exitoso", "Informacion", "Registro"));
        }
        return resultado;
    }

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuenta validacion)
    {
        _usuarioMPP.ReemplazarValidacionEmail(usuario, validacion);
    }

    public async Task<ResultadoInicioSesionDosFactores> IniciarSesion(Usuario usuarioEntrante, bool mantenerSesion)
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

        ConfiguracionAutenticacionDosFactores? configuracionDosFactores = _usuarioMPP.ConsultarAutenticacionDosFactores(usuario);
        if (configuracionDosFactores is not null && configuracionDosFactores.TotpHabilitado)
        {
            string tokenDesafio = _seguridad.GenerarTokenSeguro();
            DateTime fechaExpiracionDesafio = DateTime.Now.AddMinutes(5);
            _usuarioMPP.CrearDesafioAutenticacionDosFactores(usuario, _seguridad.GenerarHashToken(tokenDesafio), mantenerSesion, fechaExpiracionDesafio);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "IniciarSesion2FA", "Se solicitó la verificación del segundo factor para iniciar sesión.", "Pendiente", "Informacion", "Seguridad"));
            return new ResultadoInicioSesionDosFactores(null, true, tokenDesafio, fechaExpiracionDesafio);
        }

        InicioSesionResultado resultado = CrearInicioSesion(usuario, mantenerSesion);
        return new ResultadoInicioSesionDosFactores(resultado, false, null, null);
    }

    public EstadoAutenticacionDosFactores ConsultarEstadoAutenticacionDosFactores(Usuario usuario)
    {
        ConfiguracionAutenticacionDosFactores? configuracion = _usuarioMPP.ConsultarAutenticacionDosFactores(usuario);
        return new EstadoAutenticacionDosFactores(configuracion?.TotpHabilitado ?? false, configuracion?.FechaActivacionTotp);
    }

    public PreparacionTotp PrepararActivacionTotp(Usuario usuario)
    {
        if (!string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Confirmá tu correo electrónico antes de activar Google Authenticator.");
        string claveManual = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        DateTime fechaExpiracion = DateTime.Now.AddMinutes(10);
        _usuarioMPP.GuardarAutenticacionDosFactoresPendiente(usuario, _fieldEncryptionService.CifrarSecretoAutenticacionDosFactores(claveManual), fechaExpiracion);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "PrepararGoogleAuthenticator", "El usuario inició la activación de Google Authenticator.", "Pendiente", "Informacion", "Seguridad"));
        string etiqueta = Uri.EscapeDataString("TeamBalance:" + usuario.Email);
        string emisor = Uri.EscapeDataString("TeamBalance");
        return new PreparacionTotp($"otpauth://totp/{etiqueta}?secret={claveManual}&issuer={emisor}&algorithm=SHA1&digits=6&period=30", claveManual, fechaExpiracion);
    }

    public List<string> ConfirmarActivacionTotp(Usuario usuario, string codigo)
    {
        ConfiguracionAutenticacionDosFactores? configuracion = _usuarioMPP.ConsultarAutenticacionDosFactores(usuario);
        if (configuracion is null || string.IsNullOrWhiteSpace(configuracion.SecretoPendienteCifrado) || configuracion.FechaExpiracionPendiente is null || configuracion.FechaExpiracionPendiente < DateTime.Now || configuracion.IntentosPendientes >= 5)
            throw new InvalidOperationException("La configuración pendiente venció o alcanzó el límite de intentos. Generá un código QR nuevo.");
        string secreto = _fieldEncryptionService.DescifrarSecretoAutenticacionDosFactores(configuracion.SecretoPendienteCifrado);
        Totp totp = new Totp(Base32Encoding.ToBytes(secreto), 30, OtpHashMode.Sha1, 6);
        if (!totp.VerifyTotp(codigo?.Trim(), out long pasoTotp, new VerificationWindow(1, 1)))
        {
            _usuarioMPP.RegistrarIntentoAutenticacionDosFactoresPendiente(usuario);
            throw new UnauthorizedAccessException("El código no es válido. Verificá la hora de tu dispositivo e intentá nuevamente.");
        }
        _usuarioMPP.ActivarAutenticacionDosFactores(usuario, _fieldEncryptionService.CifrarSecretoAutenticacionDosFactores(secreto), pasoTotp);
        List<string> codigos = GenerarCodigosRecuperacion();
        _usuarioMPP.ReemplazarCodigosRecuperacionDosFactores(usuario, codigos.Select(_seguridad.GenerarHashToken).ToList());
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "ActivarGoogleAuthenticator", "Se activó Google Authenticator para la cuenta.", "Exitoso", "Informacion", "Seguridad"));
        return codigos;
    }

    public InicioSesionResultado CompletarInicioSesionTotp(string tokenDesafio, string codigo)
    {
        string tokenHash = _seguridad.GenerarHashToken(tokenDesafio);
        DesafioAutenticacionDosFactores desafio = _usuarioMPP.ConsumirDesafioAutenticacionDosFactores(tokenHash, false);
        if (!desafio.Valido || desafio.IdUsuario is null) throw new UnauthorizedAccessException("El desafío de verificación venció o ya fue utilizado.");
        Usuario usuario = _usuarioMPP.ConsultarUsuarioPorId(new Usuario { ID = desafio.IdUsuario.Value }) ?? throw new UnauthorizedAccessException("No se encontró la cuenta asociada al desafío.");
        if (!ValidarSegundoFactor(usuario, codigo)) throw new UnauthorizedAccessException("El código no es válido, venció o ya fue utilizado.");
        DesafioAutenticacionDosFactores desafioCompletado = _usuarioMPP.ConsumirDesafioAutenticacionDosFactores(tokenHash, true);
        if (!desafioCompletado.Valido) throw new UnauthorizedAccessException("El desafío de verificación ya no está vigente.");
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "CompletarInicioSesion2FA", "El usuario completó la verificación de Google Authenticator.", "Exitoso", "Informacion", "Seguridad"));
        return CrearInicioSesion(usuario, desafio.MantenerSesion);
    }

    public void DesactivarAutenticacionDosFactores(Usuario usuario, string codigo)
    {
        if (!ValidarSegundoFactor(usuario, codigo)) throw new UnauthorizedAccessException("El código no es válido o ya fue utilizado.");
        _usuarioMPP.DesactivarAutenticacionDosFactores(usuario);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "DesactivarGoogleAuthenticator", "Se desactivó Google Authenticator para la cuenta.", "Exitoso", "Advertencia", "Seguridad"));
    }

    private InicioSesionResultado CrearInicioSesion(Usuario usuario, bool mantenerSesion)
    {
        long versionAutorizacion = _usuarioMPP.ConsultarVersionAutorizacion(usuario);
        _rolBLL.CargarAutorizacion(usuario);
        if (usuario.Roles.Count == 0) throw new UnauthorizedAccessException("Tu cuenta no tiene un rol activo asignado.");

        DateTime fechaExpiracion = mantenerSesion ? DateTime.Now.AddDays(DuracionSesionRecordadaDias) : DateTime.Now.AddHours(DuracionSesionNormalHoras);

        DateTime fechaInicio = DateTime.Now;
        SesionUsuario sesion = new SesionUsuario(0, usuario.ID, string.Empty, fechaInicio, fechaInicio, fechaExpiracion, null, true, null);
        sesion.VersionAutorizacion = versionAutorizacion;
        string accessToken = _fieldEncryptionService.GenerarTokenSesion(usuario, sesion);
        sesion.TokenHash = _seguridad.GenerarHashToken(accessToken);

        _usuarioMPP.RegistrarSesion(sesion);

        Bitacora bitacora = new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario",usuario.ID, "IniciarSesion", "El usuario inició sesión en TeamBalance.","Exitoso","Informacion","Seguridad");
        _bitacoraBLL.Add(bitacora);

        return new InicioSesionResultado(usuario, accessToken, fechaExpiracion);
    }

    private static List<string> GenerarCodigosRecuperacion()
    {
        List<string> codigos = new List<string>();
        for (int indice = 0; indice < 10; indice++) codigos.Add(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(5)));
        return codigos;
    }

    private bool ValidarSegundoFactor(Usuario usuario, string codigo)
    {
        string codigoNormalizado = codigo?.Trim().Replace(" ", string.Empty).ToUpperInvariant() ?? string.Empty;
        ConfiguracionAutenticacionDosFactores? configuracion = _usuarioMPP.ConsultarAutenticacionDosFactores(usuario);
        if (configuracion is null || !configuracion.TotpHabilitado || string.IsNullOrWhiteSpace(configuracion.SecretoTotpCifrado)) return false;
        if (!System.Text.RegularExpressions.Regex.IsMatch(codigoNormalizado, "^\\d{6}$")) return _usuarioMPP.ConsumirCodigoRecuperacionDosFactores(usuario, _seguridad.GenerarHashToken(codigoNormalizado));
        Totp totp = new Totp(Base32Encoding.ToBytes(_fieldEncryptionService.DescifrarSecretoAutenticacionDosFactores(configuracion.SecretoTotpCifrado)), 30, OtpHashMode.Sha1, 6);
        bool valido = totp.VerifyTotp(codigoNormalizado, out long pasoTotp, new VerificationWindow(1, 1));
        if (!valido || (configuracion.UltimoPasoTotp is not null && pasoTotp <= configuracion.UltimoPasoTotp.Value)) return false;
        _usuarioMPP.ActualizarUltimoPasoTotp(usuario, pasoTotp);
        return true;
    }

    public bool SesionVigente(string accessToken)
    {
        return ConsultarUsuarioSesion(accessToken) is not null;
    }

    public Usuario? ConsultarUsuarioSesion(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        Usuario? usuario = _fieldEncryptionService.ConsultarUsuarioToken(accessToken);
        return usuario is not null && _usuarioMPP.SesionVigente(_seguridad.GenerarHashToken(accessToken)) ? usuario : null;
    }

    public Usuario? ConsultarAutorizacionSesion(string accessToken)
    {
        Usuario? usuario = ConsultarUsuarioSesion(accessToken);
        return usuario is null ? null : _usuarioMPP.CargarFotoPerfil(usuario);
    }

    public void CerrarSesion(string accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            Usuario? usuario = ConsultarUsuarioSesion(accessToken);
            _usuarioMPP.CerrarSesion(_seguridad.GenerarHashToken(accessToken));
            if (usuario is not null)
            {
                RegistrarEventoSeguridad(usuario, "CerrarSesion", "El usuario cerró su sesión en TeamBalance.", "Exitoso", "Informacion");
            }
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
        await ValidarPasswordConServicio(usuario.PasswordHash);
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
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Usuario", usuario.ID, "ModificarUsuarioAgencia", "Se modificaron los datos y roles de un usuario de la agencia.", "Exitoso", "Informacion", "Usuarios"));
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
                List<Tarea> tareasPendientes = _tareaMPP.Consultar(solicitante, new FiltroTarea(null, null, null, idEmpleado, null, null, null))
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

                bool correoEnviado = await _emailService.EnviarCorreoRecuperoPassword(usuario, token);

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

        await ValidarPasswordConServicio(usuario.PasswordHash);
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

        bool correoEnviado = await _emailService.EnviarCorreoPasswordModificada(usuarioBD);
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

        bool correoEnviado = await _emailService.EnviarCorreoPasswordModificada(usuarioBD);
        RegistrarEventoSeguridad(usuarioBD, "CambiarPassword", correoEnviado ? "El usuario modificó su contraseña desde una sesión autenticada." : "El usuario modificó su contraseña, pero no se pudo enviar el correo de confirmación.", correoEnviado ? "Exitoso" : "Parcial", correoEnviado ? "Informacion" : "Advertencia");
    }

    private async Task ValidarPasswordConServicio(string password)
    {
        PasswordEvaluationResponse evaluacion = await _passwordSecurityBLL.Evaluar(password);
        if (evaluacion.Valida) { return; }

        List<string> faltantes = new();
        if (!evaluacion.Requisitos.Longitud) faltantes.Add("al menos 8 caracteres");
        if (!evaluacion.Requisitos.Mayuscula) faltantes.Add("una mayúscula");
        if (!evaluacion.Requisitos.Minuscula) faltantes.Add("una minúscula");
        if (!evaluacion.Requisitos.Numero) faltantes.Add("un número");
        if (!evaluacion.Requisitos.CaracterEspecial) faltantes.Add("un signo");
        throw new ArgumentException($"La contraseña debe incluir {string.Join(", ", faltantes)}.");
    }

    public string ActualizarFotoPerfil(Usuario solicitante, string? fotoPerfil)
    {
        if (solicitante.ID <= 0)
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
        }

        string fotoValidada = ValidarFotoPerfil(fotoPerfil);
        solicitante.FotoPerfil = fotoValidada;
        _usuarioMPP.ActualizarFotoPerfil(solicitante);
        RegistrarEventoSeguridad(solicitante, "ActualizarFotoPerfil", "El usuario actualizó su foto de perfil.", "Exitoso", "Informacion");
        return fotoValidada;
    }

    private static string ValidarFotoPerfil(string? fotoPerfil)
    {
        if (string.IsNullOrWhiteSpace(fotoPerfil))
        {
            throw new ArgumentException("Seleccioná una imagen para usar como foto de perfil.");
        }

        Match coincidencia = FotoPerfilDataUrlRegex.Match(fotoPerfil);
        if (!coincidencia.Success)
        {
            throw new ArgumentException("La foto debe ser una imagen PNG, JPEG o WebP válida.");
        }

        byte[] contenido;
        try { contenido = Convert.FromBase64String(coincidencia.Groups[2].Value); }
        catch (FormatException) { throw new ArgumentException("La foto de perfil no tiene un formato válido."); }

        if (contenido.Length == 0 || contenido.Length > TamanoMaximoFotoPerfilBytes || !CoincideFirmaImagen(coincidencia.Groups[1].Value, contenido))
        {
            throw new ArgumentException("La foto debe ser una imagen válida de hasta 512 KB.");
        }

        return fotoPerfil;
    }

    private static bool CoincideFirmaImagen(string tipoContenido, byte[] contenido)
    {
        return tipoContenido switch
        {
            "image/png" => contenido.Length >= 8 && contenido[0] == 0x89 && contenido[1] == 0x50 && contenido[2] == 0x4E && contenido[3] == 0x47,
            "image/jpeg" => contenido.Length >= 3 && contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF,
            "image/webp" => contenido.Length >= 12 && contenido[0] == 0x52 && contenido[1] == 0x49 && contenido[2] == 0x46 && contenido[3] == 0x46 && contenido[8] == 0x57 && contenido[9] == 0x45 && contenido[10] == 0x42 && contenido[11] == 0x50,
            _ => false,
        };
    }

    private void RegistrarEventoSeguridad(Usuario? usuario, string accion, string mensaje, string resultado, string criticidad)
    {
        Bitacora bitacora = new Bitacora(0, usuario?.ID, usuario?.IdAgencia, "Usuario", usuario?.ID, accion, mensaje, resultado, criticidad, "Seguridad", DateTime.Now, null);

        _bitacoraBLL.Add(bitacora);
    }

}
