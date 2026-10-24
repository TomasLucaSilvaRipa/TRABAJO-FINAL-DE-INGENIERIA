using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/idiomas")]
public class IdiomasController : ControllerBase
{
    private readonly BLLIdioma _idiomaBLL;
    private readonly BLLUsuario _usuarioBLL;
    public IdiomasController(BLLIdioma idiomaBLL, BLLUsuario usuarioBLL) { _idiomaBLL = idiomaBLL; _usuarioBLL = usuarioBLL; }

    [HttpGet("activos")]
    public IActionResult ConsultarActivos() => Ok(_idiomaBLL.ConsultarActivos());

    [HttpGet("gestion")]
    public IActionResult ConsultarGestion()
    {
        try { return Ok(_idiomaBLL.ConsultarGestion(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost]
    public IActionResult Registrar([FromBody] Idioma idioma)
    {
        try { return Ok(_idiomaBLL.Registrar(idioma, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible registrar el idioma."); }
    }

    [HttpPut("{idIdioma:int}")]
    public IActionResult Modificar(int idIdioma, [FromBody] Idioma idioma)
    {
        try { idioma.ID = idIdioma; _idiomaBLL.Modificar(idioma, ObtenerSolicitante()); return NoContent(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible guardar el idioma."); }
    }

    [HttpDelete("{idIdioma:int}")]
    public IActionResult DarDeBaja(int idIdioma)
    {
        try
        {
            Idioma idioma = new Idioma(idIdioma);
            _idiomaBLL.DarDeBaja(idioma, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible dar de baja el idioma."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
