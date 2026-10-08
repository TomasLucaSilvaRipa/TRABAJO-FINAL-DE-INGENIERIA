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

    [HttpGet("skills/areas")]
    public IActionResult ConsultarAreasSkills()
    {
        try { return Ok(_recursosBLL.ConsultarAreasSkills(ObtenerSolicitante())); }
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

    [HttpGet("empleados/{idUsuario:int}/ficha")]
    public IActionResult ConsultarFichaEmpleado([FromRoute] int idUsuario)
    {
        try
        {
            if (idUsuario <= 0) { return BadRequest("Seleccioná un empleado válido."); }
            return Ok(_recursosBLL.ConsultarFichaEmpleado(new Usuario { ID = idUsuario }, ObtenerSolicitante()));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("empleados/{idUsuario:int}/ficha/exportacion")]
    public IActionResult RegistrarExportacionFichaEmpleado([FromRoute] int idUsuario)
    {
        try
        {
            _recursosBLL.RegistrarExportacionFichaEmpleado(idUsuario, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("empleados/{idUsuario:int}/ficha")]
    public IActionResult GuardarFichaEmpleado([FromRoute] int idUsuario, [FromBody] FichaEmpleadoActualizar ficha)
    {
        try
        {
            Empleado empleado = new Empleado
            {
                ID = idUsuario,
                EmpleadoSkills = ficha.EmpleadoSkills ?? new List<EmpleadoSkill>(),
                DisponibilidadBase = ficha.DisponibilidadBase,
            };
            _recursosBLL.GuardarFichaEmpleado(empleado, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
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
        catch (Exception ex) { return BadRequest(ex.Message); }
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
        try { ResultadoGestionDisponibilidad resultado = _recursosBLL.RegistrarAusencia(ausencia, ObtenerSolicitante()); return Ok(resultado); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("ausencias/pendientes")]
    public IActionResult ConsultarSolicitudesPendientes()
    {
        try { return Ok(_recursosBLL.ConsultarSolicitudesPendientes(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("ausencias/empleados")]
    public IActionResult ConsultarEmpleadosDisponibilidad()
    {
        try { return Ok(_recursosBLL.ConsultarEmpleadosDisponibilidad(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("ausencias/{idAusencia:int}/aprobar")]
    public IActionResult AprobarAusencia([FromRoute] int idAusencia, [FromBody] ResolucionAusenciaEmpleado resolucion)
    {
        try { resolucion.IdAusencia = idAusencia; return Ok(_recursosBLL.AprobarAusencia(resolucion, ObtenerSolicitante(), false)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("ausencias/{idAusencia:int}/aprobar-parcial")]
    public IActionResult AprobarParcialmenteAusencia([FromRoute] int idAusencia, [FromBody] ResolucionAusenciaEmpleado resolucion)
    {
        try { resolucion.IdAusencia = idAusencia; return Ok(_recursosBLL.AprobarAusencia(resolucion, ObtenerSolicitante(), true)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("ausencias/{idAusencia:int}/rechazar")]
    public IActionResult RechazarAusencia([FromRoute] int idAusencia, [FromBody] ResolucionAusenciaEmpleado resolucion)
    {
        try { resolucion.IdAusencia = idAusencia; return Ok(_recursosBLL.RechazarAusencia(resolucion, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("ausencias/directa")]
    public IActionResult RegistrarAusenciaDirecta([FromBody] AusenciaEmpleado ausencia)
    {
        try { return Ok(_recursosBLL.RegistrarAusenciaDirecta(ausencia, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    private Usuario ObtenerSolicitante()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }
}
