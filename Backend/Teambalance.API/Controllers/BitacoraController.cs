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
    public IActionResult LeerBitacora(
        [FromQuery] int? idAgencia = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int? idUsuario = null,
        [FromQuery] string? entidad = null,
        [FromQuery] string? accion = null,
        [FromQuery] string? resultado = null,
        [FromQuery] string? criticidad = null,
        [FromQuery] string? modulo = null)
    {
        try
        {
            bool tieneFiltros =
                desde.HasValue ||
                hasta.HasValue ||
                idUsuario.HasValue ||
                !string.IsNullOrWhiteSpace(entidad) ||
                !string.IsNullOrWhiteSpace(accion) ||
                !string.IsNullOrWhiteSpace(resultado) ||
                !string.IsNullOrWhiteSpace(criticidad) ||
                !string.IsNullOrWhiteSpace(modulo);

            List<Bitacora> bitacora;

            if (tieneFiltros)
            {
                bitacora = _bllBitacora.FiltrarBitacora(
                    idAgencia,
                    desde,
                    hasta,
                    idUsuario,
                    entidad,
                    accion,
                    resultado,
                    criticidad,
                    modulo
                );
            }
            else
            {
                bitacora =
                    _bllBitacora.LeerBitacora(idAgencia);
            }

            return Ok(bitacora);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(
                500,
                "No fue posible consultar la bitácora."
            );
        }
    }
}