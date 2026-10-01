using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/riesgos-retraso")]
public class RiesgosRetrasoController : ControllerBase
{
    private readonly BLLRiesgoRetraso _riesgoBLL;
    private readonly BLLUsuario _usuarioBLL;

    public RiesgosRetrasoController(BLLRiesgoRetraso riesgoBLL, BLLUsuario usuarioBLL)
    {
        _riesgoBLL = riesgoBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpPost("consultar")]
    public IActionResult Consultar([FromBody] FiltroRiesgoRetraso filtro)
    {
        try
        {
            ResultadoRiesgoRetraso resultado = _riesgoBLL.ConsultarRiesgo(filtro, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        try
        {
            string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);
            if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
            return usuario;
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
