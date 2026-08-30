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
            Usuario usuario = new Usuario()
            {
                Email = datosLogin.Email,
                PasswordHash = datosLogin.Password,
                RecaptchaToken = request.RecaptchaToken,
            };

            (Usuario Usuario, string AccessToken, DateTime FechaExpiracion) resultado = await _usuarioBLL.IniciarSesion(usuario, mantenerSesion);

            return Ok(new
            {
                accessToken = resultado.AccessToken,
                expiresAt = resultado.FechaExpiracion,
                usuario = new
                {
                    id = resultado.Usuario.ID,
                    nombre = resultado.Usuario.Nombre,
                    apellido = resultado.Usuario.Apellido,
                    email = resultado.Usuario.Email,
                    idAgencia = resultado.Usuario.IdAgencia,
                    idRol = resultado.Usuario.IdRol,
                },
            });
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
