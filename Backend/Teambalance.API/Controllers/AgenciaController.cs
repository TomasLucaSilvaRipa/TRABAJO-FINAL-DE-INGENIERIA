using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[Route("api/agencias")]
public class AgenciaController : ControllerBase
{
    private readonly BLLAgencia _agenciaBLL;

    public AgenciaController(BLLAgencia agenciaBLL)
    {
        _agenciaBLL = agenciaBLL;
    }

    [HttpPost("{referenciaContratacion}/registro")]
    public async Task<IActionResult> RegistrarAgencia(string referenciaContratacion, [FromBody] Agencia agencia)
    {
        try
        {
            Usuario? usuario = agencia.Usuarios.FirstOrDefault();

            if (usuario is null)
            {
                return BadRequest("No se recibieron los datos del dueño.");
            }

            bool emailValidacionEnviado = await _agenciaBLL.RegistrarAgencia(agencia, usuario, referenciaContratacion);

            return Ok(new
            {
                mensaje = "La agencia y el usuario Dueño fueron registrados correctamente.",
                emailValidacionEnviado,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible completar el registro de la agencia.");
        }
    }

    [HttpPost("validar-cuenta")]
    public IActionResult ValidarCuenta([FromQuery] string token)
    {
        try
        {
            if (!_agenciaBLL.ValidarCuenta(token))
            {
                return BadRequest("El enlace de validación es inválido, ya fue utilizado o venció.");
            }

            return Ok(new { mensaje = "Tu correo fue confirmado. Ya podés iniciar sesión en TeamBalance." });
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible validar la cuenta en este momento.");
        }
    }

    [HttpPost("reenvio-validacion")]
    public async Task<IActionResult> ReenviarValidacion([FromBody] Usuario usuario)
    {
        try
        {
            await _agenciaBLL.ReenviarValidacion(usuario.Email ?? string.Empty);

            return Ok(new { mensaje = "Si existe una cuenta pendiente asociada a ese email, enviamos un nuevo enlace de validación." });
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible procesar el reenvío de validación.");
        }
    }
}
