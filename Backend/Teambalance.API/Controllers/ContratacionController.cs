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
        public async Task<IActionResult> Contratar(ContratacionServicio contratacion)
        {
            var resultado = await _bll.Contratar(contratacion);

            return Ok(resultado);
        }
    }
}
