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
    [HttpGet("skills")] public IActionResult ConsultarSkills() { try { return Ok(_recursosBLL.ConsultarSkills(ObtenerSolicitante())); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpPost("skills")] public IActionResult RegistrarSkill([FromBody] Skill skill) { try { Skill resultado = _recursosBLL.RegistrarSkill(skill, ObtenerSolicitante()); return Ok(resultado); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPatch("skills/{idSkill:int}/estado")] public IActionResult CambiarEstadoSkill(int idSkill, [FromQuery] bool activo) { try { return Ok(_recursosBLL.CambiarEstadoSkill(idSkill, activo, ObtenerSolicitante())); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpGet("empleados/{idUsuario:int}/ficha")] public IActionResult ConsultarFichaEmpleado(int idUsuario) { try { return Ok(_recursosBLL.ConsultarFichaEmpleado(idUsuario, ObtenerSolicitante())); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPut("empleados/{idUsuario:int}/ficha")] public IActionResult GuardarFichaEmpleado(int idUsuario, [FromBody] Empleado empleado) { try { _recursosBLL.GuardarFichaEmpleado(idUsuario, empleado, ObtenerSolicitante()); return NoContent(); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    private Usuario ObtenerSolicitante() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
}
