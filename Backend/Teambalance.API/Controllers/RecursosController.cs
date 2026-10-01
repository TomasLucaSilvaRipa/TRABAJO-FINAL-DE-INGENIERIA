using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/recursos")]
public class RecursosController : ControllerBase
{
    private readonly BLLRecursos _recursosBLL; private readonly BLLUsuario _usuarioBLL;
    public RecursosController(BLLRecursos recursosBLL, BLLUsuario usuarioBLL) { _recursosBLL = recursosBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet("skills")]
    public IActionResult ConsultarSkills()
    {
        try
        {
            return Ok(_recursosBLL.ConsultarSkills(ObtenerSolicitante()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost("skills")]
    public IActionResult RegistrarSkill([FromBody] Skill skill)
    {
        try
        {
            Skill resultado = _recursosBLL.RegistrarSkill(skill, ObtenerSolicitante());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPatch("skills/{idSkill:int}/estado")]
    public IActionResult CambiarEstadoSkill([FromBody] Skill skill)
    {
        try
        {
            return Ok(_recursosBLL.CambiarEstadoSkill(skill, ObtenerSolicitante()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpGet("empleados/{ID:int}/ficha")]
    public IActionResult ConsultarFichaEmpleado([FromRoute] Usuario usuario)
    {
        try
        {
            return Ok(_recursosBLL.ConsultarFichaEmpleado(usuario, ObtenerSolicitante()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("empleados/{ID:int}/ficha")]
    public IActionResult GuardarFichaEmpleado([FromBody] Empleado empleado)
    {
        try
        {
            _recursosBLL.GuardarFichaEmpleado(empleado, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("disponibilidad")]
    public IActionResult ConsultarMiDisponibilidad()
    {
        try
        {
            Empleado empleado = _recursosBLL.ConsultarMiDisponibilidad(ObtenerSolicitante());
            return Ok(empleado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut("disponibilidad")]
    public IActionResult GuardarMiDisponibilidad([FromBody] DisponibilidadBase disponibilidad)
    {
        try
        {
            _recursosBLL.GuardarMiDisponibilidad(disponibilidad, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("ausencias")]
    public IActionResult ConsultarMisAusencias()
    {
        try { List<AusenciaEmpleado> ausencias = _recursosBLL.ConsultarMisAusencias(ObtenerSolicitante()); return Ok(ausencias); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("ausencias")]
    public IActionResult RegistrarAusencia([FromBody] AusenciaEmpleado ausencia)
    {
        try { AusenciaEmpleado resultado = _recursosBLL.RegistrarAusencia(ausencia, ObtenerSolicitante()); return Ok(resultado); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ObtenerSolicitante()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }
}
