using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers
{
    [ApiController]
    [Route("api/contratacion")]
    public class ContratacionController : ControllerBase
    {
        public readonly ContratacionBLL _bll;

        public ContratacionController(ContratacionBLL bll)
        {
            _bll = bll;
        }

        [HttpPost]
        public async Task<IActionResult> Contratar([FromBody] ContratacionRequest request)
        {
            try
            {
                var resultado = await _bll.Contratar(request);

                return Ok(resultado);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{referencia}/estado")]
        public IActionResult ConsultarEstado(string referencia)
        {
            try
            {
                return Ok(_bll.ConsultarEstado(referencia));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("{referencia}/verificar-pago")]
        public async Task<IActionResult> VerificarPago(string referencia, [FromBody] VerificarPagoRequest request)
        {
            try
            {
                return Ok(await _bll.VerificarPagoMercadoPago(referencia, request.PaymentId));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }

    public sealed class VerificarPagoRequest
    {
        public string PaymentId { get; init; } = string.Empty;
    }
}
