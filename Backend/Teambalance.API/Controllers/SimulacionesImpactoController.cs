using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/simulaciones-impacto")]
public class SimulacionesImpactoController : ControllerBase
{
    private readonly BLLSimulacionImpacto _simulacionBLL;
    private readonly BLLUsuario _usuarioBLL;

    public SimulacionesImpactoController(BLLSimulacionImpacto simulacionBLL, BLLUsuario usuarioBLL)
    {
        _simulacionBLL = simulacionBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("tareas")]
    public IActionResult ConsultarTareasDisponibles()
    {
        try
        {
            return Ok(_simulacionBLL.ConsultarTareasDisponibles(ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("calcular")]
    public IActionResult Calcular([FromBody] SimulacionImpacto simulacion)
    {
        try
        {
            return Ok(_simulacionBLL.Calcular(simulacion, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost]
    public IActionResult Conservar([FromBody] SimulacionImpacto simulacion)
    {
        try
        {
            SimulacionImpacto resultado = _simulacionBLL.Conservar(simulacion, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("descartar")]
    public IActionResult Descartar([FromBody] SimulacionImpacto simulacion)
    {
        try
        {
            return Ok(_simulacionBLL.Descartar(simulacion, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);

        if (usuario is null)
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
        }

        return usuario;
    }
}
