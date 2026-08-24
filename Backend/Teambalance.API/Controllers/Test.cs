namespace Teambalance.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using TeamBalance.BLL;



    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("conexion")]
        public IActionResult ProbarConexion()
        {
            string? cadenaConexion =
                _configuration.GetConnectionString("TeamBalanceDB");

            if (string.IsNullOrEmpty(cadenaConexion))
            {
                return StatusCode(500, "No se encontró la cadena de conexión.");
            }

            TestBLL bll = new TestBLL(cadenaConexion);

            bool resultado = bll.ProbarConexion();

            if (resultado)
            {
                return Ok("Conexión con TeamBalance correcta.");
            }

            return StatusCode(500, "No se pudo conectar con TeamBalance.");
        }
    }

}
