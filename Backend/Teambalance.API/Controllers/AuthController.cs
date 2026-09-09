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

            EncryptionService.LoginDecryptedData datosLogin = _encryptionService.DesencriptarLogin(request.EncryptedData, request.EncryptedKey, request.Iv);
            Usuario usuario = new Usuario(0, null, new Rol(), string.Empty, string.Empty, datosLogin.Email, datosLogin.Password, string.Empty, DateTime.MinValue, false);
            usuario.RecaptchaToken = request.RecaptchaToken;

            InicioSesionResultado resultado = await _usuarioBLL.IniciarSesion(usuario, mantenerSesion);

            List<RolResponse> roles = resultado.Usuario.Roles.Select(rol => new RolResponse(rol.ID, rol.Nombre)).ToList();
            List<PermisoResponse> permisos = resultado.Usuario.Permisos.Select(permiso => new PermisoResponse(permiso.ID, permiso.Codigo, permiso.Nombre, permiso.Url)).ToList();
            RolResponse rolPrincipal = new RolResponse(resultado.Usuario.Rol.ID, resultado.Usuario.Rol.Nombre);
            UsuarioSesionResponse usuarioResponse = new UsuarioSesionResponse(resultado.Usuario.ID, resultado.Usuario.IdAgencia, rolPrincipal, resultado.Usuario.Nombre, resultado.Usuario.Apellido, resultado.Usuario.Email, roles, permisos);
            LoginResponse response = new LoginResponse(resultado.AccessToken, resultado.FechaExpiracion, usuarioResponse);
            return Ok(response);
        }
        catch (System.Security.Cryptography.CryptographicException){ return BadRequest("No fue posible descifrar las credenciales recibidas."); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return StatusCode(500, ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible iniciar sesión en este momento."); }
    }

    [HttpGet("sesion")]
    public IActionResult ValidarSesion()
    {
        string? accessToken = ObtenerAccessToken();

        if (!_usuarioBLL.SesionVigente(accessToken ?? string.Empty))
        {
            return Unauthorized();
        }

        return Ok(new { vigente = true });
    }

    [HttpGet("autorizacion")]
    public IActionResult ConsultarAutorizacion()
    {
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(ObtenerAccessToken() ?? string.Empty);
        if (usuario is null)
        {
            return Unauthorized();
        }
        List<RolResponse> roles = usuario.Roles.Select(rol => new RolResponse(rol.ID, rol.Nombre)).ToList();
        List<PermisoResponse> permisos = usuario.Permisos.Select(permiso => new PermisoResponse(permiso.ID, permiso.Codigo, permiso.Nombre, permiso.Url)).ToList();
        RolResponse rolPrincipal = new RolResponse(usuario.Rol.ID, usuario.Rol.Nombre);
        UsuarioSesionResponse usuarioResponse = new UsuarioSesionResponse(usuario.ID, usuario.IdAgencia, rolPrincipal, usuario.Nombre, usuario.Apellido, usuario.Email, roles, permisos);
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

            return Ok(new { mensaje = "Si existe una cuenta activa asociada a ese email, enviamos un enlace para restablecer la contraseña." });
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

            return Ok(new { mensaje = "La contraseña fue restablecida correctamente. Volvé a iniciar sesión." });
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

            return Ok(new { mensaje = "La contraseña fue modificada correctamente. Volvé a iniciar sesión." });
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
}
