using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/simulaciones-impacto")]
public class SimulacionesImpactoController : ControllerBase
{
    private readonly BLLSimulacionImpacto _simulacionBLL; private readonly BLLUsuario _usuarioBLL;
    public SimulacionesImpactoController(BLLSimulacionImpacto simulacionBLL, BLLUsuario usuarioBLL) { _simulacionBLL = simulacionBLL; _usuarioBLL = usuarioBLL; }
    [HttpPost] public IActionResult Simular([FromBody] SimulacionImpacto simulacion) { try { SimulacionImpacto resultado = _simulacionBLL.Simular(simulacion, ValidarSesion()); return Ok(resultado); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } catch (ArgumentException ex) { return BadRequest(ex.Message); } catch (Exception ex) { return StatusCode(500, ex.Message); } }
    private Usuario ValidarSesion() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token); if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); } return usuario; }
}
