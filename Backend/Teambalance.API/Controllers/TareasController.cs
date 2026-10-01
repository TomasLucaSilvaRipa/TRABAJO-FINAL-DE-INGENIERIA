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
    [HttpGet] public IActionResult Consultar()
    {
        try
        {
            return Ok(_tareaBLL.Consultar(ValidarSesion()));
        }catch(Exception ex) { throw new Exception(ex.Message); }
    }

    [HttpGet("mias")] 
    public IActionResult ConsultarAsignadas() { 
        try 
        { 
            List<Tarea> tareas = _tareaBLL.ConsultarAsignadas(ValidarSesion()); 
            return Ok(tareas); 
        } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } 
    }
    [HttpGet("proyectos-pm")]
    public IActionResult ConsultarPorPM()
    {
        try { List<Tarea> tareas = _tareaBLL.ConsultarPorPM(ValidarSesion()); return Ok(tareas); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
    
    [HttpPatch("mias/comentarios")]
    public IActionResult GuardarComentarios([FromBody] Tarea tarea)
    {
        try
        {
            bool resultado = _tareaBLL.GuardarComentarios(tarea, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("opciones")] 
    public IActionResult Opciones() { 
        return Ok(_tareaBLL.ConsultarOpciones(ValidarSesion())); 
    }
    [HttpPost] public IActionResult Guardar([FromBody] Tarea tarea) 
    { 
        try 
        { 
            Tarea resultado = _tareaBLL.Guardar(tarea, ValidarSesion()); 
            return Ok(resultado); 
        } catch (ArgumentException ex) { return BadRequest(ex.Message); } 
    }
    [HttpPut("{id:int}")] public IActionResult Modificar([FromBody] Tarea tarea) 
    { 
        try 
        {
            Tarea resultado = _tareaBLL.Guardar(tarea, ValidarSesion()); 
            return Ok(resultado); 
        } 
        catch (ArgumentException ex) { return BadRequest(ex.Message); } 
    }

    [HttpPatch("estado")]
    public IActionResult CambiarEstado([FromBody] Tarea tarea) 
    {
        return Ok(_tareaBLL.CambiarEstado(tarea, ValidarSesion())); 
    }
    [HttpPost("skills")] 
    public IActionResult RegistrarSkill([FromBody] Skill skill) { 
        try 
        { 
            Skill resultado = _tareaBLL.RegistrarSkill(skill, ValidarSesion()); return Ok(resultado); 
        } 
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } 
        catch (ArgumentException ex) { return BadRequest(ex.Message); } 
    }

    [HttpPatch("skills/estado")] 
    public IActionResult CambiarEstadoSkill([FromBody]Skill skill) { 
        try 
        {
            return Ok(_tareaBLL.CambiarEstadoSkill(skill, ValidarSesion())); 
        } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } 
    }

    [HttpPatch("mias/estado")]
    public IActionResult CambiarEstadoPropio([FromBody] Tarea tarea)
    {
        try
        {
            bool resultado = _tareaBLL.CambiarEstadoPropio(tarea, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion() 
    {
        try
        {
            string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);
            if (usuario is null)
            {
                throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
            }
            return usuario;
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }  
    }
}

