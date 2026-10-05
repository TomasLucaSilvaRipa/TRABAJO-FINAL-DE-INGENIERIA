using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/plantillas-tareas")]
public class PlantillasTareasController : ControllerBase
{
    private readonly BLLPlantillaTarea _plantillaBLL;
    private readonly BLLUsuario _usuarioBLL;

    public PlantillasTareasController(BLLPlantillaTarea plantillaBLL, BLLUsuario usuarioBLL)
    {
        _plantillaBLL = plantillaBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet]
    public IActionResult Consultar([FromQuery] bool incluirInactivas = false)
    {
        try
        {
            return Ok(_plantillaBLL.Consultar(ValidarSesion(), incluirInactivas));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpGet("tareas-base")]
    public IActionResult ConsultarTareasBase()
    {
        try
        {
            return Ok(_plantillaBLL.ConsultarTareasBase(ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("desde-tarea")]
    public IActionResult CrearDesdeTarea([FromBody] Tarea tarea)
    {
        try
        {
            return Ok(_plantillaBLL.CrearDesdeTarea(tarea, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost]
    public IActionResult Crear([FromBody] PlantillaTarea plantilla)
    {
        try
        {
            return Ok(_plantillaBLL.Guardar(plantilla, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut]
    public IActionResult Modificar([FromBody] PlantillaTarea plantilla)
    {
        try
        {
            return Ok(_plantillaBLL.Guardar(plantilla, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("baja")]
    public IActionResult DarBaja([FromBody] PlantillaTarea plantilla)
    {
        try
        {
            return Ok(_plantillaBLL.DarBaja(plantilla, ValidarSesion()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);

        if (usuario is null)
        {
            throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
        }

        return usuario;
    }
}
