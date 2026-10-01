using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/reportes-ejecutivos")]
public class ReportesEjecutivosController : ControllerBase
{
    private readonly BLLReporteEjecutivo _reporteBLL;
    private readonly BLLUsuario _usuarioBLL;

    public ReportesEjecutivosController(BLLReporteEjecutivo reporteBLL, BLLUsuario usuarioBLL)
    {
        _reporteBLL = reporteBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpPost("previsualizar")]
    public IActionResult Previsualizar([FromBody] ConfiguracionReporteEjecutivo configuracion)
    {
        try
        {
            ReporteEjecutivo reporte = _reporteBLL.PrevisualizarReporte(configuracion, ValidarSesion());
            return Ok(reporte);
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
