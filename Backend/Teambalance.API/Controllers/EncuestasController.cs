using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/encuestas")]
public sealed class EncuestasController : ControllerBase
{
    private readonly BLLEncuesta _encuestaBLL;
    private readonly BLLUsuario _usuarioBLL;

    public EncuestasController(BLLEncuesta encuestaBLL, BLLUsuario usuarioBLL)
    {
        _encuestaBLL = encuestaBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("publicas")]
    public IActionResult ConsultarPublicas()
    {
        return Ok(_encuestaBLL.ConsultarPublicas());
    }

    [HttpGet("publicas/{ID:int}")]
    public IActionResult ConsultarPublica([FromRoute] Encuesta encuesta)
    {
        return EjecutarPublico(() => Ok(_encuestaBLL.ConsultarPublica(encuesta)));
    }

    [HttpPost("publicas/{ID:int}/respuestas")]
    public IActionResult Responder([FromRoute] Encuesta encuesta, [FromBody] RespuestaEncuesta respuesta)
    {
        return EjecutarPublico(() =>
        {
            respuesta.IdEncuesta = encuesta.ID;
            _encuestaBLL.Responder(respuesta);
            return NoContent();
        });
    }

    [HttpGet("gestion")]
    public IActionResult ConsultarGestion()
    {
        return EjecutarGestion(() => Ok(_encuestaBLL.ConsultarGestion(UsuarioActual())));
    }

    [HttpGet("gestion/{ID:int}")]
    public IActionResult ConsultarDetalleGestion([FromRoute] Encuesta encuesta)
    {
        return EjecutarGestion(() => Ok(_encuestaBLL.ConsultarGestion(encuesta, UsuarioActual())));
    }

    [HttpPost("gestion")]
    public IActionResult Guardar([FromBody] Encuesta encuesta)
    {
        return EjecutarGestion(() => Ok(_encuestaBLL.Guardar(encuesta, UsuarioActual())));
    }

    [HttpPost("gestion/{ID:int}/baja")]
    public IActionResult DarDeBaja([FromRoute] Encuesta encuesta)
    {
        return EjecutarGestion(() =>
        {
            _encuestaBLL.DarDeBaja(encuesta, UsuarioActual());
            return NoContent();
        });
    }

    [HttpGet("gestion/{ID:int}/resultados")]
    public IActionResult ConsultarResultados([FromRoute] Encuesta encuesta)
    {
        return EjecutarGestion(() => Ok(_encuestaBLL.ConsultarResultados(encuesta, UsuarioActual())));
    }

    private Usuario UsuarioActual()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }

    private IActionResult EjecutarPublico(Func<IActionResult> accion)
    {
        try { return accion(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible procesar la encuesta."); }
    }

    private IActionResult EjecutarGestion(Func<IActionResult> accion)
    {
        try { return accion(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible administrar las encuestas."); }
    }
}
