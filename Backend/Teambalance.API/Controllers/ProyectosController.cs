using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;
[ApiController]
[Route("api/proyectos")]
public class ProyectosController : ControllerBase
{
    private readonly BLLProyecto _proyectoBLL; private readonly BLLUsuario _usuarioBLL;
    public ProyectosController(BLLProyecto proyectoBLL, BLLUsuario usuarioBLL) { _proyectoBLL = proyectoBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet] public IActionResult Consultar() { return Ok(_proyectoBLL.Consultar(ValidarSesion())); }
    [HttpGet("opciones")] public IActionResult Opciones() { return Ok(_proyectoBLL.ConsultarOpciones(ValidarSesion())); }
    [HttpPost] public IActionResult Guardar([FromBody] Proyecto proyecto) { try { Proyecto resultado = _proyectoBLL.Guardar(proyecto, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPut("{id:int}")] public IActionResult Modificar(int id, [FromBody] Proyecto proyecto) { try { proyecto.ID = id; Proyecto resultado = _proyectoBLL.Guardar(proyecto, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPatch("{id:int}/estado")] public IActionResult CambiarEstado(int id, [FromQuery] bool activo) { return Ok(_proyectoBLL.CambiarEstado(id, activo, ValidarSesion())); }
    [HttpPost("clientes")] public IActionResult RegistrarCliente([FromBody] Cliente cliente) { try { Cliente resultado = _proyectoBLL.RegistrarCliente(cliente, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPut("clientes/{id:int}")] public IActionResult ModificarCliente(int id, [FromBody] Cliente cliente) { try { cliente.ID = id; Cliente resultado = _proyectoBLL.ModificarCliente(cliente, ValidarSesion()); return Ok(resultado); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }
    [HttpPatch("clientes/{id:int}/estado")] public IActionResult CambiarEstadoCliente(int id, [FromQuery] bool activo) { return Ok(_proyectoBLL.CambiarEstadoCliente(id, activo, ValidarSesion())); }
    private Usuario ValidarSesion() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token); if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); } return usuario; }
}
