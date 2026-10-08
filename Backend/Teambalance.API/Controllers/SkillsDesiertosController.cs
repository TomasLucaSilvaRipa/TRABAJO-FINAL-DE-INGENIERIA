using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/skills-desiertos")]
public class SkillsDesiertosController : ControllerBase
{
    private readonly BLLSkillsDesiertos _skillsBLL;
    private readonly BLLUsuario _usuarioBLL;
    public SkillsDesiertosController(BLLSkillsDesiertos skillsBLL, BLLUsuario usuarioBLL) { _skillsBLL = skillsBLL; _usuarioBLL = usuarioBLL; }

    [HttpGet("resumen")]
    public IActionResult ConsultarResumen()
    {
        try { return Ok(_skillsBLL.AnalizarCobertura(new FiltroCoberturaSkill(), ObtenerSolicitante(), false).Take(3)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("analizar")]
    public IActionResult Analizar([FromBody] FiltroCoberturaSkill filtro)
    {
        try { return Ok(_skillsBLL.AnalizarCobertura(filtro, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("detalle/{idSkill:int}")]
    public IActionResult ConsultarDetalle([FromRoute] int idSkill, [FromBody] FiltroCoberturaSkill filtro)
    {
        try { return Ok(_skillsBLL.ConsultarDetalle(idSkill, filtro, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost("recomendaciones")]
    public IActionResult RegistrarRecomendacion([FromBody] RecomendacionSkill recomendacion)
    {
        try { return Ok(_skillsBLL.RegistrarRecomendacion(recomendacion, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("sugerencias")]
    public IActionResult SugerirEmpleados([FromBody] ConsultaSugerenciasSkill consulta)
    {
        try { return Ok(_skillsBLL.SugerirEmpleados(consulta, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
