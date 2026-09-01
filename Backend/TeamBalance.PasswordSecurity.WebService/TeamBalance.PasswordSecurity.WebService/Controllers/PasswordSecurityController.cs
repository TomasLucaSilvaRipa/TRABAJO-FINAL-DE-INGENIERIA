using Microsoft.AspNetCore.Mvc;
using TeamBalance.PasswordSecurity.WebService.Models;
using TeamBalance.PasswordSecurity.WebService.Services;

namespace TeamBalance.PasswordSecurity.WebService.Controllers;

[ApiController]
[Route("api/password-security")]
public class PasswordSecurityController : ControllerBase
{
    private readonly PasswordEvaluationService _passwordEvaluationService;

    public PasswordSecurityController(
        PasswordEvaluationService passwordEvaluationService)
    {
        _passwordEvaluationService = passwordEvaluationService;
    }

    [HttpPost("evaluar")]
    public IActionResult Evaluar([FromBody] PasswordEvaluationRequest request)
    {
        try
        {
            PasswordEvaluationResponse resultado = _passwordEvaluationService.Evaluar(request.Password);
            return Ok(resultado);
        }
        catch (Exception){ return StatusCode(500,"No fue posible evaluar la contraseña."); }
    }
}