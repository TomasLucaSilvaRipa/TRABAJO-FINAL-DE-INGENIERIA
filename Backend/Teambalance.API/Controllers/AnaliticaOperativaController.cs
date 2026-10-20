using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/analitica-operativa")]
public class AnaliticaOperativaController : ControllerBase
{
    private readonly BLLAnaliticaOperativa _analiticaBLL;
    private readonly BLLUsuario _usuarioBLL;

    public AnaliticaOperativaController(BLLAnaliticaOperativa analiticaBLL, BLLUsuario usuarioBLL)
    {
        _analiticaBLL = analiticaBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("ocupacion")]
    public IActionResult ConsultarOcupacion([FromQuery] FiltroOcupacionOperativa filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarOcupacion(ObtenerSolicitante(), filtro)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la ocupación operativa."); }
    }

    [HttpGet("ocupacion/detalle-empleado")]
    public IActionResult ConsultarDetalleOcupacionEmpleado([FromQuery] FiltroOcupacionOperativa filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarDetalleOcupacionEmpleado(ObtenerSolicitante(), filtro)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar el detalle de ocupación."); }
    }

    [HttpPost("ocupacion/solicitudes-revision")]
    public async Task<IActionResult> SolicitarRevisionOperativa([FromBody] SolicitudRevisionOperativaNueva solicitud)
    {
        try { await _analiticaBLL.SolicitarRevisionOperativa(ObtenerSolicitante(), solicitud); return Ok("Solicitud de revisión enviada correctamente"); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible enviar la solicitud de revisión."); }
    }

    [HttpGet("ocupacion/solicitudes-revision/pendientes")]
    public IActionResult ConsultarSolicitudesRevisionPendientes()
    {
        try { return Ok(_analiticaBLL.ConsultarSolicitudesRevisionPendientes(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar las revisiones pendientes."); }
    }

    [HttpGet("desvio-horas")]
    public IActionResult ConsultarDesvioHoras([FromQuery] FiltroDesvioHoras filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarDesvioHoras(ObtenerSolicitante(), filtro)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar el desvío de horas."); }
    }

    [HttpGet("desvio-horas/detalle-tarea")]
    public IActionResult ConsultarDetalleDesvioHorasTarea([FromQuery] int idProyecto, [FromQuery] int idTarea, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        try { return Ok(_analiticaBLL.ConsultarDetalleDesvioHorasTarea(ObtenerSolicitante(), idProyecto, idTarea, desde, hasta)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar el detalle de la tarea."); }
    }

    [HttpPost("desvio-horas/acciones-ajuste")]
    public IActionResult RegistrarAccionAjuste([FromBody] AccionAjusteDesvioHoras accion)
    {
        try { _analiticaBLL.RegistrarAccionAjusteDesvioHoras(ObtenerSolicitante(), accion); return Ok("La acción de ajuste quedó vinculada al desvío consultado."); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible registrar la acción de ajuste."); }
    }

    [HttpPost("desvio-horas/comunicar-dueno")]
    public async Task<IActionResult> ComunicarDesvioADueno([FromBody] ComunicacionDesvioHoras comunicacion)
    {
        try { await _analiticaBLL.ComunicarDesvioHorasADueno(ObtenerSolicitante(), comunicacion); return Ok("El desvío fue comunicado al Dueño y el envío quedó registrado."); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible comunicar el desvío al Dueño."); }
    }

    [HttpGet("conveniencia")]
    public IActionResult ConsultarConveniencia()
    {
        try { return Ok(_analiticaBLL.ConsultarConveniencia(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la conveniencia operativa."); }
    }

    [HttpGet("sobrecalificacion")]
    public IActionResult ConsultarSobrecalificacion()
    {
        try { return Ok(_analiticaBLL.ConsultarSobrecalificacion(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la sobrecalificación de perfiles."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
