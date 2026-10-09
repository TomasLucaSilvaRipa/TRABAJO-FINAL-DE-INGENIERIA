using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/faqs")]
public sealed class PreguntasFrecuentesController : ControllerBase
{
    private readonly BLLPreguntaFrecuente _preguntaFrecuenteBLL;
    private readonly BLLUsuario _usuarioBLL;

    public PreguntasFrecuentesController(BLLPreguntaFrecuente preguntaFrecuenteBLL, BLLUsuario usuarioBLL) {
        _preguntaFrecuenteBLL = preguntaFrecuenteBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("publicas")]
    public IActionResult ConsultarPublicas() {
        return Ok(_preguntaFrecuenteBLL.ConsultarPublicas());
    }

    [HttpGet("gestion")]
    public IActionResult ConsultarGestion() {
        return Ejecutar(() => Ok(_preguntaFrecuenteBLL.ConsultarGestion(UsuarioActual())));
    }

    [HttpPost]
    public IActionResult Guardar([FromBody] PreguntaFrecuente pregunta) {
        return Ejecutar(() => Ok(_preguntaFrecuenteBLL.Guardar(pregunta, UsuarioActual())));
    }

    [HttpPost("{ID:int}/baja")]
    public IActionResult DarDeBaja([FromRoute] PreguntaFrecuente pregunta) {
        return Ejecutar(() => {
            _preguntaFrecuenteBLL.DarDeBaja(pregunta, UsuarioActual());
            return NoContent();
        });
    }

    private Usuario UsuarioActual() {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }

    private IActionResult Ejecutar(Func<IActionResult> accion) {
        try { return accion(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible administrar las preguntas frecuentes."); }
    }
}
