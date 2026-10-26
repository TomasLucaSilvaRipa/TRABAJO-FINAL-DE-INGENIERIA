using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/slack")]
public sealed class SlackController : ControllerBase
{
    private const long VentanaAntireplaySegundos = 300;
    private readonly BLLSlack _slackBLL;
    private readonly BLLUsuario _usuarioBLL;
    private readonly IConfiguration _configuration;

    public SlackController(BLLSlack slackBLL, BLLUsuario usuarioBLL, IConfiguration configuration)
    {
        _slackBLL = slackBLL;
        _usuarioBLL = usuarioBLL;
        _configuration = configuration;
    }

    [HttpGet("usuarios-sin-vincular")]
    public IActionResult ConsultarUsuariosSinVincular()
    {
        try
        {
            List<TeamBalance.BE.Entidades.Usuario> usuarios = _slackBLL.ConsultarUsuariosSinVincular(ObtenerSolicitante());
            foreach (TeamBalance.BE.Entidades.Usuario usuario in usuarios)
            {
                usuario.PasswordHash = string.Empty;
                usuario.PasswordActual = null;
                usuario.RecaptchaToken = null;
                usuario.Permisos.Clear();
                usuario.Roles.Clear();
            }

            return Ok(usuarios);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible consultar los usuarios disponibles para Slack.");
        }
    }

    [HttpPost("usuarios/{idUsuario:int}/vinculacion")]
    public IActionResult VincularUsuario(int idUsuario, [FromBody] string slackUserId)
    {
        try
        {
            _slackBLL.VincularUsuario(new VinculacionSlack
            {
                Usuario = new TeamBalance.BE.Entidades.Usuario { ID = idUsuario },
                IdUsuarioSlack = slackUserId
            }, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible vincular el usuario con Slack.");
        }
    }

    [HttpPost("balance")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> ConsultarBalance()
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest("El comando debe recibirse como formulario.");
        }

        Request.EnableBuffering();
        string cuerpo = await LeerCuerpoCrudo();
        if (!ValidarFirma(cuerpo))
        {
            return Unauthorized();
        }

        Request.Body.Position = 0;
        IFormCollection formulario = await Request.ReadFormAsync();
        if (!string.Equals(formulario["command"], "/balance", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Comando no admitido.");
        }

        try
        {
            string respuesta = _slackBLL.ConsultarBalance(new ConsultaSlack
            {
                IdUsuarioSlack = formulario["user_id"].ToString(),
                Texto = formulario["text"].ToString()
            });
            return Ok(new RespuestaSlack("ephemeral", respuesta));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Ok(new RespuestaSlack("ephemeral", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Ok(new RespuestaSlack("ephemeral", ex.Message));
        }
        catch (Exception)
        {
            return Ok(new RespuestaSlack("ephemeral", "No fue posible consultar la información operativa en este momento."));
        }
    }

    private async Task<string> LeerCuerpoCrudo()
    {
        using StreamReader lector = new StreamReader(Request.Body, Encoding.UTF8, false, 1024, true);
        string cuerpo = await lector.ReadToEndAsync();
        Request.Body.Position = 0;
        return cuerpo;
    }

    private TeamBalance.BE.Entidades.Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string accessToken = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(accessToken) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }

    private bool ValidarFirma(string cuerpo)
    {
        string signingSecret = _configuration["Slack:SigningSecret"] ?? string.Empty;
        string timestamp = Request.Headers["X-Slack-Request-Timestamp"].ToString();
        string firmaRecibida = Request.Headers["X-Slack-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signingSecret) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(firmaRecibida) || !long.TryParse(timestamp, out long timestampUnix))
        {
            return false;
        }

        long diferencia = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestampUnix);
        if (diferencia > VentanaAntireplaySegundos)
        {
            return false;
        }

        string baseFirma = "v0:" + timestamp + ":" + cuerpo;
        byte[] clave = Encoding.UTF8.GetBytes(signingSecret);
        byte[] datos = Encoding.UTF8.GetBytes(baseFirma);
        byte[] hash;
        using (HMACSHA256 hmac = new HMACSHA256(clave))
        {
            hash = hmac.ComputeHash(datos);
        }

        string firmaEsperada = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();
        byte[] bytesEsperados = Encoding.UTF8.GetBytes(firmaEsperada);
        byte[] bytesRecibidos = Encoding.UTF8.GetBytes(firmaRecibida);
        return bytesEsperados.Length == bytesRecibidos.Length && CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecibidos);
    }
}
