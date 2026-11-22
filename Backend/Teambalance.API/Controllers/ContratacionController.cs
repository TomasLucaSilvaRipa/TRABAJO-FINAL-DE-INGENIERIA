using Teambalance.API.Models;
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
                ContratacionInicioResponse resultado = await _bll.Contratar(request);
                return Ok(resultado);
            }
            catch (DatosContratacionEnUsoException ex) { return Conflict(new { code = "datos_contratacion_en_uso", message = ex.Message }); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("planes/{idPlan:int}/cotizacion-pago")]
        public IActionResult CotizarPagoCombinado(int idPlan)
        {
            try { return Ok(_bll.CotizarPagoCombinado(new PlanComercial(idPlan))); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception) { return BadRequest("No fue posible cotizar el pago combinado."); }
        }

        [HttpPost("{referencia}/continuar")]
        public async Task<IActionResult> ContinuarPagoCombinado([ReferenciaHttp("referencia", nameof(ContratacionServicio.ReferenciaContratacion))] ContratacionServicio contratacion)
        {
            try { return Ok(await _bll.ContinuarPagoCombinado(contratacion)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception) { return BadRequest("No fue posible continuar el pago combinado."); }
        }

        [HttpGet("{referencia}/estado")]
        public IActionResult ConsultarEstado([ReferenciaHttp("referencia", nameof(ContratacionServicio.ReferenciaContratacion))] ContratacionServicio contratacion)
        {
            try { return Ok(_bll.ConsultarEstado(contratacion)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("{referencia}/verificar-pago")]
        public async Task<IActionResult> VerificarPago([ReferenciaHttp("referencia", nameof(ContratacionServicio.ReferenciaContratacion))] ContratacionServicio contratacion, [FromBody] VerificarPagoRequest request)
        {
            try { return Ok(await _bll.VerificarPagoMercadoPago(contratacion, request.PaymentId)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
