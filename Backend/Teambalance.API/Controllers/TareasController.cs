using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;
[ApiController]
[Route("api/tareas")]
public class TareasController : ControllerBase
{
    private readonly BLLTarea _tareaBLL; private readonly BLLUsuario _usuarioBLL;
    public TareasController(BLLTarea tareaBLL, BLLUsuario usuarioBLL) { _tareaBLL = tareaBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet] public IActionResult Consultar() { return Ok(_tareaBLL.Consultar(ValidarSesion())); }
    [HttpGet("mias")] public IActionResult ConsultarAsignadas() { try { List<Tarea> tareas = _tareaBLL.ConsultarAsignadas(ValidarSesion()); return Ok(tareas); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpPatch("mias/{idTarea:int}/comentarios")] public IActionResult GuardarComentarios(int idTarea, [FromBody] ComentariosTareaRequest request) { try { bool resultado = _tareaBLL.GuardarComentarios(idTarea, request.ComentariosJson ?? "[]", ValidarSesion()); return Ok(resultado); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpGet("opciones")] public IActionResult Opciones() { return Ok(_tareaBLL.ConsultarOpciones(ValidarSesion())); }
    [HttpPost] public IActionResult Guardar([FromBody] Tarea tarea) { try { Tarea resultado = _tareaBLL.Guardar(tarea, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPut("{id:int}")] public IActionResult Modificar(int id, [FromBody] Tarea tarea) { try { tarea.ID = id; Tarea resultado = _tareaBLL.Guardar(tarea, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPatch("{id:int}/estado")] public IActionResult CambiarEstado(int id, [FromQuery] bool activo) { return Ok(_tareaBLL.CambiarEstado(id, activo, ValidarSesion())); }
    [HttpPost("skills")] public IActionResult RegistrarSkill([FromBody] Skill skill) { try { Skill resultado = _tareaBLL.RegistrarSkill(skill, ValidarSesion()); return Ok(resultado); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPatch("skills/{idSkill:int}/estado")] public IActionResult CambiarEstadoSkill(int idSkill, [FromQuery] bool activo) { try { return Ok(_tareaBLL.CambiarEstadoSkill(idSkill, activo, ValidarSesion())); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    private Usuario ValidarSesion() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token); if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); } return usuario; }
}

public class ComentariosTareaRequest { public ComentariosTareaRequest() { } public ComentariosTareaRequest(string comentariosJson) { ComentariosJson = comentariosJson; } public string? ComentariosJson { get; set; } }
