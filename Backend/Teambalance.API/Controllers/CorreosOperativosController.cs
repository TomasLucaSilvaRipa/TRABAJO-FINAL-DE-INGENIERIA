using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/correos-operativos")]
public class CorreosOperativosController : ControllerBase
{
    private readonly BLLCorreoOperativo _correoBLL;
    private readonly BLLUsuario _usuarioBLL;
    public CorreosOperativosController(BLLCorreoOperativo correoBLL, BLLUsuario usuarioBLL) { _correoBLL = correoBLL; _usuarioBLL = usuarioBLL; }

    [HttpGet("preferencias")]
    public IActionResult ConsultarPreferencias()
    {
        try { return Ok(_correoBLL.ConsultarPreferencia(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPut("preferencias")]
    public IActionResult GuardarPreferencias([FromBody] PreferenciaCorreoOperativo preferencia)
    {
        try { return Ok(_correoBLL.GuardarPreferencia(preferencia, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("acciones/{token}")]
    public IActionResult ConsultarAccion(string token)
    {
        try { return Ok(_correoBLL.ConsultarAccion(token)); }
        catch (ArgumentException ex) { return NotFound(ex.Message); }
    }

    [HttpPost("acciones/{token}")]
    public IActionResult EjecutarAccion(string token)
    {
        try { return Ok(_correoBLL.EjecutarAccion(token)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ObtenerSolicitante()
    {
        string authorization = Request.Headers.Authorization.ToString();
        string token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
