using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/bitacora")]
public class BitacoraController : ControllerBase
{
    private readonly BLLBitacora _bllBitacora;
    private readonly BLLUsuario _usuarioBLL;

    public BitacoraController(BLLBitacora bllBitacora, BLLUsuario usuarioBLL)
    {
        _bllBitacora = bllBitacora;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet]
    public IActionResult LeerBitacora([FromQuery] FiltroBitacora filtro)
    {
        try
        {
            bool tieneFiltros = filtro.Desde.HasValue || filtro.Hasta.HasValue || filtro.IdUsuario.HasValue || !string.IsNullOrWhiteSpace(filtro.Entidad) || !string.IsNullOrWhiteSpace(filtro.Accion) || filtro.Resultado.HasValue || filtro.Criticidad.HasValue || filtro.Modulo.HasValue;

            List<Bitacora> bitacora;

            if (tieneFiltros)
            {
                bitacora = _bllBitacora.FiltrarBitacora(filtro);
            }
            else
            {
                bitacora =_bllBitacora.LeerBitacora(filtro);
            }

            return Ok(bitacora);
        }
        catch (ArgumentException ex){return BadRequest(ex.Message);}
        catch (Exception){return StatusCode(500,"No fue posible consultar la bitácora.");}
    }

    [HttpPost("informe")]
    public async Task<IActionResult> GenerarInforme([FromBody] FiltroBitacora filtro)
    {
        try
        {
            await _bllBitacora.GenerarYEnviarInforme(filtro, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible generar y enviar el informe de bitácora."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string accessToken = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(accessToken) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
