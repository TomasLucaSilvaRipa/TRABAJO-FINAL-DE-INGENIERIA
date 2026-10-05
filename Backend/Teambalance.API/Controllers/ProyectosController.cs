using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/proyectos")]
public class ProyectosController : ControllerBase
{
    private readonly BLLProyecto _proyectoBLL;
    private readonly BLLUsuario _usuarioBLL;

    public ProyectosController(BLLProyecto proyectoBLL, BLLUsuario usuarioBLL)
    {
        _proyectoBLL = proyectoBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet]
    public IActionResult Consultar()
    {
        try
        {
            List<Proyecto> proyectos = _proyectoBLL.Consultar(ValidarSesion());
            return Ok(proyectos);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("opciones")]
    public IActionResult Opciones()
    {
        try
        {
            GestionProyectoOpciones opciones = _proyectoBLL.ConsultarOpciones(ValidarSesion());
            return Ok(opciones);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("detalle")]
    public IActionResult ConsultarDetalle([FromBody] Proyecto proyecto)
    {
        try
        {
            Proyecto resultado = _proyectoBLL.ConsultarDetalle(proyecto, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("filtrar")]
    public IActionResult Filtrar([FromBody] FiltroProyecto filtro)
    {
        try
        {
            List<Proyecto> proyectos = _proyectoBLL.Consultar(ValidarSesion(), filtro);
            return Ok(proyectos);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost]
    public IActionResult Guardar([FromBody] Proyecto proyecto)
    {
        try
        {
            Proyecto resultado = _proyectoBLL.Guardar(proyecto, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut]
    public IActionResult Modificar([FromBody] Proyecto proyecto)
    {
        try
        {
            Proyecto resultado = _proyectoBLL.Guardar(proyecto, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("estado")]
    public IActionResult CambiarEstado([FromBody] Proyecto proyecto)
    {
        try
        {
            bool resultado = _proyectoBLL.CambiarEstado(proyecto, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("cerrar")]
    public IActionResult Cerrar([FromBody] Proyecto proyecto)
    {
        try
        {
            bool resultado = _proyectoBLL.Cerrar(proyecto, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("clientes")]
    public IActionResult RegistrarCliente([FromBody] Cliente cliente)
    {
        try
        {
            Cliente resultado = _proyectoBLL.RegistrarCliente(cliente, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut("clientes")]
    public IActionResult ModificarCliente([FromBody] Cliente cliente)
    {
        try
        {
            Cliente resultado = _proyectoBLL.ModificarCliente(cliente, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("clientes/estado")]
    public IActionResult CambiarEstadoCliente([FromBody] Cliente cliente)
    {
        try
        {
            bool resultado = _proyectoBLL.CambiarEstadoCliente(cliente, ValidarSesion());
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
            if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
            return usuario;
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
