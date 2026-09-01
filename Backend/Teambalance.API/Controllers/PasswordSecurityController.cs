using Microsoft.AspNetCore.Mvc;
using TeamBalance.BLL;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/password-security")]
public class PasswordSecurityController : ControllerBase
{
    private readonly BLLPasswordSecurity _bllPasswordSecurity;

    public PasswordSecurityController(BLLPasswordSecurity bllPasswordSecurity)
    {
        _bllPasswordSecurity = bllPasswordSecurity;
    }

    [HttpPost("evaluar")]
    public async Task<IActionResult> Evaluar([FromBody] PasswordRequest request)
    {
        try
        {
            PasswordEvaluationResponse resultado = await _bllPasswordSecurity.Evaluar(request.Password);

            return Ok(resultado);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (HttpRequestException) { return StatusCode(503, "El servicio de seguridad de contraseñas no está disponible."); }
        catch (Exception) { return StatusCode(500, "No fue posible evaluar la contraseña."); }
    }
}

public class PasswordRequest
{
    public string Password { get; set; } = string.Empty;
}