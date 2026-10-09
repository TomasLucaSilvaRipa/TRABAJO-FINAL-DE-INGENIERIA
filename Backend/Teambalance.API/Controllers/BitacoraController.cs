using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/bitacora")]
public class BitacoraController : ControllerBase
{
    private readonly BLLBitacora _bllBitacora;

    public BitacoraController(BLLBitacora bllBitacora)
    {
        _bllBitacora = bllBitacora;
    }

    [HttpGet]
    public IActionResult LeerBitacora([FromQuery] FiltroBitacora filtro)
    {
        try
        {
            bool tieneFiltros = filtro.Desde.HasValue || filtro.Hasta.HasValue || filtro.IdUsuario.HasValue || !string.IsNullOrWhiteSpace(filtro.Entidad) || !string.IsNullOrWhiteSpace(filtro.Accion) || filtro.Resultado.HasValue || filtro.Criticidad.HasValue || !string.IsNullOrWhiteSpace(filtro.Modulo);

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
}
