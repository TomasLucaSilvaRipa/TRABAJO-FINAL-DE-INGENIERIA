using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/best-fit")]
public class BestFitController : ControllerBase
{
    private readonly BLLBestFit _bestFitBLL; private readonly BLLUsuario _usuarioBLL;
    public BestFitController(BLLBestFit bestFitBLL, BLLUsuario usuarioBLL) { _bestFitBLL = bestFitBLL; _usuarioBLL = usuarioBLL; }

    [HttpPost("sugerencias")]
    public IActionResult Sugerir([FromBody] RecomendacionBestFit recomendacion)
    {
        try
        {
            List<RecomendacionBestFit> resultados = _bestFitBLL.Sugerir(recomendacion, ValidarSesion());
            return Ok(resultados);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("seleccion")]
    public IActionResult GuardarRecomendacion([FromBody] RecomendacionBestFit recomendacion)
    {
        try { bool resultado = _bestFitBLL.GuardarRecomendacion(recomendacion, ValidarSesion()); return Ok(resultado); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("tareas/{idTarea}/recomendaciones")]
    public IActionResult ConsultarRecomendaciones(int idTarea)
    {
        try { return Ok(_bestFitBLL.ConsultarRecomendaciones(new Tarea { ID = idTarea }, ValidarSesion())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);
        if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
        return usuario;
    }
}
