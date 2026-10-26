using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using TeamBalance.Services;
using Teambalance.API.Models;

namespace Teambalance.API.Controllers;

[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly BLLUsuario _usuarioBLL;
    private readonly EncryptionService _encryptionService;

    public AuthController(BLLUsuario usuarioBLL, EncryptionService encryptionService)
    {
        _usuarioBLL = usuarioBLL;
        _encryptionService = encryptionService;
    }

    [HttpGet("public-key")]
    public IActionResult ObtenerClavePublica()
    {
        return Content(_encryptionService.ObtenerClavePublica(), "text/plain");
    }

    [HttpPost("login")]
    public async Task<IActionResult> IniciarSesion([FromBody] LoginEncryptedRequest request, [FromQuery] bool mantenerSesion = false)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.EncryptedData) || string.IsNullOrWhiteSpace(request.EncryptedKey) || string.IsNullOrWhiteSpace(request.Iv) || string.IsNullOrWhiteSpace(request.RecaptchaToken))
            {
                throw new ArgumentException("No fue posible recibir las credenciales de inicio de sesión.");
            }

            CredencialCifrada credencial = new()
            {
                DatosCifrados = request.EncryptedData,
                ClaveCifrada = request.EncryptedKey,
                VectorInicializacion = request.Iv,
                TokenRecaptcha = request.RecaptchaToken
            };
            LoginDecryptedData datosLogin = _encryptionService.DesencriptarLogin(credencial);
            Usuario usuario = new Usuario(0, null, new Rol(), string.Empty, string.Empty, datosLogin.Email, datosLogin.Password, string.Empty, DateTime.MinValue, false);
            usuario.RecaptchaToken = request.RecaptchaToken;

            ResultadoInicioSesionDosFactores resultado = await _usuarioBLL.IniciarSesion(usuario, mantenerSesion);
            if (resultado.RequiereDosFactores && !string.IsNullOrWhiteSpace(resultado.TokenDesafio) && resultado.FechaExpiracionDesafio is not null)
            {
                return Accepted(new DesafioInicioSesionDosFactoresResponse(resultado.TokenDesafio, resultado.FechaExpiracionDesafio.Value));
            }
            if (resultado.InicioSesion is null) throw new InvalidOperationException("No fue posible completar el inicio de sesión.");
            return Ok(CrearLoginResponse(resultado.InicioSesion));
        }
        catch (System.Security.Cryptography.CryptographicException){ return BadRequest("No fue posible descifrar las credenciales recibidas."); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return StatusCode(500, ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible iniciar sesión en este momento."); }
    }

    [HttpPost("login/confirmar-2fa")]
    public IActionResult ConfirmarInicioSesionDosFactores([FromBody] ConfirmarInicioSesionDosFactoresRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.TokenDesafio) || string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá el código de verificación.");
            InicioSesionResultado resultado = _usuarioBLL.CompletarInicioSesionTotp(request.TokenDesafio, request.Codigo);
            return Ok(CrearLoginResponse(resultado));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible verificar el segundo factor."); }
    }

    [HttpGet("2fa")]
    public IActionResult ConsultarAutenticacionDosFactores()
    {
        Usuario usuario = ObtenerUsuarioAutenticado();
        return Ok(_usuarioBLL.ConsultarEstadoAutenticacionDosFactores(usuario));
    }

    [HttpPost("2fa/preparar")]
    public IActionResult PrepararAutenticacionDosFactores()
    {
        try { return Ok(_usuarioBLL.PrepararActivacionTotp(ObtenerUsuarioAutenticado())); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("2fa/confirmar")]
    public IActionResult ConfirmarAutenticacionDosFactores([FromBody] CodigoAutenticacionDosFactoresRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá el código de seis dígitos.");
            List<string> codigosRecuperacion = _usuarioBLL.ConfirmarActivacionTotp(ObtenerUsuarioAutenticado(), request.Codigo);
            return Ok(new RespuestaCodigosRecuperacion(codigosRecuperacion));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("2fa/desactivar")]
    public IActionResult DesactivarAutenticacionDosFactores([FromBody] CodigoAutenticacionDosFactoresRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá un código de Authenticator o de recuperación.");
            _usuarioBLL.DesactivarAutenticacionDosFactores(ObtenerUsuarioAutenticado(), request.Codigo);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("sesion")]
    public IActionResult ValidarSesion()
    {
        string? accessToken = ObtenerAccessToken();

        if (!_usuarioBLL.SesionVigente(accessToken ?? string.Empty))
        {
            return Unauthorized();
        }

        return Ok(new RespuestaVigencia(true));
    }

    [HttpGet("autorizacion")]
    public IActionResult ConsultarAutorizacion()
    {
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(ObtenerAccessToken() ?? string.Empty);
        if (usuario is null)
        {
            return Unauthorized();
        }
        List<RolResponse> roles = usuario.Roles.Select(rol => new RolResponse(rol.ID, rol.Nombre, rol.TipoUsuario)).ToList();
        List<PermisoResponse> permisos = usuario.Permisos.Select(permiso => new PermisoResponse(permiso.ID, permiso.Codigo, permiso.Nombre, permiso.Url)).ToList();
        RolResponse rolPrincipal = new RolResponse(usuario.Rol.ID, usuario.Rol.Nombre, usuario.Rol.TipoUsuario);
        UsuarioSesionResponse usuarioResponse = new UsuarioSesionResponse(usuario.ID, usuario.IdAgencia, rolPrincipal, usuario.Nombre, usuario.Apellido, usuario.Email, usuario.FotoPerfil, roles, permisos);
        AutorizacionResponse response = new AutorizacionResponse(usuarioResponse, roles, permisos);
        return Ok(response);
    }

    [HttpPost("logout")]
    public IActionResult CerrarSesion()
    {
        _usuarioBLL.CerrarSesion(ObtenerAccessToken() ?? string.Empty);

        return NoContent();
    }

    [HttpPost("recuperar-password")]
    public async Task<IActionResult> SolicitarRecuperoPassword([FromBody] Usuario usuario)
    {
        try
        {
            await _usuarioBLL.SolicitarRecuperoPassword(usuario);

            return Ok(new RespuestaMensaje("Si existe una cuenta activa asociada a ese email, enviamos un enlace para restablecer la contraseña."));
        }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return StatusCode(500, ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible procesar la solicitud de recuperación en este momento."); }
    }

    [HttpPost("restablecer-password")]
    public async Task<IActionResult> RestablecerPassword([FromQuery] string token, [FromBody] Usuario usuario)
    {
        try
        {
            await _usuarioBLL.RestablecerPassword(usuario, token);

            return Ok(new RespuestaMensaje("La contraseña fue restablecida correctamente. Volvé a iniciar sesión."));
        }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return StatusCode(500, ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible restablecer la contraseña en este momento."); }
    }

    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] Usuario usuario)
    {
        try
        {
            await _usuarioBLL.CambiarPassword(usuario, ObtenerAccessToken() ?? string.Empty);

            return Ok(new RespuestaMensaje("La contraseña fue modificada correctamente. Volvé a iniciar sesión."));
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return StatusCode(500, ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible modificar la contraseña en este momento."); }
    }

    private string? ObtenerAccessToken()
    {
        string? authorization = Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorization[7..].Trim();
    }

    private Usuario ObtenerUsuarioAutenticado()
    {
        return _usuarioBLL.ConsultarUsuarioSesion(ObtenerAccessToken() ?? string.Empty) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }

    private static LoginResponse CrearLoginResponse(InicioSesionResultado resultado)
    {
        List<RolResponse> roles = resultado.Usuario.Roles.Select(rol => new RolResponse(rol.ID, rol.Nombre, rol.TipoUsuario)).ToList();
        List<PermisoResponse> permisos = resultado.Usuario.Permisos.Select(permiso => new PermisoResponse(permiso.ID, permiso.Codigo, permiso.Nombre, permiso.Url)).ToList();
        RolResponse rolPrincipal = new RolResponse(resultado.Usuario.Rol.ID, resultado.Usuario.Rol.Nombre, resultado.Usuario.Rol.TipoUsuario);
        UsuarioSesionResponse usuarioResponse = new UsuarioSesionResponse(resultado.Usuario.ID, resultado.Usuario.IdAgencia, rolPrincipal, resultado.Usuario.Nombre, resultado.Usuario.Apellido, resultado.Usuario.Email, resultado.Usuario.FotoPerfil, roles, permisos);
        return new LoginResponse(resultado.AccessToken, resultado.FechaExpiracion, usuarioResponse);
    }
}
