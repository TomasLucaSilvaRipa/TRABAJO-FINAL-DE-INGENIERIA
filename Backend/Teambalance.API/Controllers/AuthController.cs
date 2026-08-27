using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly BLLUsuario _usuarioBLL;

    public AuthController(BLLUsuario usuarioBLL)
    {
        _usuarioBLL = usuarioBLL;
    }

    [HttpPost("login")]
    public IActionResult IniciarSesion([FromBody] Usuario usuario, [FromQuery] bool mantenerSesion = false)
    {
        try
        {
            var resultado = _usuarioBLL.IniciarSesion(usuario, mantenerSesion);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible iniciar sesión en este momento.");
        }
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
