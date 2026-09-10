using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/opiniones")]
public class OpinionesController : ControllerBase
{
    private readonly BLLOpinionServicio _opinionBLL;
    private readonly BLLUsuario _usuarioBLL;
    public OpinionesController(BLLOpinionServicio opinionBLL, BLLUsuario usuarioBLL) { _opinionBLL = opinionBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet] public IActionResult Consultar() { List<OpinionServicio> opiniones = _opinionBLL.ConsultarPublicas(); return Ok(opiniones); }
    [HttpPost] public IActionResult Guardar([FromBody] OpinionServicio opinion)
    {
        try { Usuario usuario = ValidarSesion(); OpinionServicio resultado = _opinionBLL.Guardar(opinion, usuario); return Ok(resultado); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
    private Usuario ValidarSesion()
    {
        string accessToken = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(accessToken);
        if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión."); }
        return usuario;
    }
}
