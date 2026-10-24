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
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la ocupación operativa."); }
    }

    [HttpGet("mi-carga")]
    public IActionResult ConsultarMiCargaOperativa()
    {
        try { return Ok(_analiticaBLL.ConsultarMiCargaOperativa(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar tu carga operativa."); }
    }

    [HttpGet("ocupacion/detalle-empleado")]
    public IActionResult ConsultarDetalleOcupacionEmpleado([FromQuery] FiltroOcupacionOperativa filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarDetalleOcupacionEmpleado(ObtenerSolicitante(), filtro)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
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

    [HttpPatch("ocupacion/solicitudes-revision/{idSolicitud:int}/resolver")]
    public IActionResult MarcarSolicitudRevisionResuelta(int idSolicitud, [FromQuery] string? tipo)
    {
        try { _analiticaBLL.MarcarSolicitudRevisionResuelta(ObtenerSolicitante(), idSolicitud, tipo); return Ok("La solicitud quedó marcada como resuelta."); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible actualizar la solicitud de revisión."); }
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
    public async Task<IActionResult> RegistrarAccionAjuste([FromBody] AccionAjusteDesvioHoras accion)
    {
        try { return Ok(await _analiticaBLL.RegistrarAccionAjusteDesvioHoras(ObtenerSolicitante(), accion)); }
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
    public IActionResult ConsultarConveniencia([FromQuery] FiltroConvenienciaOperativa filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarConveniencia(ObtenerSolicitante(), filtro)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la conveniencia operativa."); }
    }

    [HttpGet("conveniencia/detalle")]
    public IActionResult ConsultarDetalleConveniencia([FromQuery] int idProyecto, [FromQuery] FiltroConvenienciaOperativa filtro)
    {
        try { return Ok(_analiticaBLL.ConsultarDetalleConveniencia(ObtenerSolicitante(), idProyecto, filtro)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar la composición del costo operativo."); }
    }

    [HttpPost("conveniencia/seguimientos")]
    public IActionResult RegistrarSeguimientoConveniencia([FromBody] NuevoSeguimientoConvenienciaOperativa seguimiento)
    {
        try { _analiticaBLL.RegistrarSeguimientoConveniencia(ObtenerSolicitante(), seguimiento); return Ok("El seguimiento quedó asociado al resultado de conveniencia."); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible registrar el seguimiento de conveniencia."); }
    }

    [HttpPost("conveniencia/clientes/{idCliente:int}/marcar-revision")]
    public async Task<IActionResult> MarcarClienteRevisionConveniencia(int idCliente)
    {
        try { return Ok(await _analiticaBLL.MarcarClienteRevisionConveniencia(ObtenerSolicitante(), idCliente)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible marcar el cliente para revisión operativa."); }
    }

    [HttpGet("sobrecalificacion")]
    public IActionResult ConsultarSobrecalificacion([FromQuery] int? idProyecto)
    {
        try { return Ok(_analiticaBLL.ConsultarSobrecalificacion(ObtenerSolicitante(), idProyecto)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible analizar la sobrecalificación de perfiles."); }
    }

    [HttpPost("sobrecalificacion/solicitudes-revision")]
    public async Task<IActionResult> SolicitarRevisionSobrecalificacion([FromBody] SolicitudRevisionSobrecalificacionNueva solicitud)
    {
        try { await _analiticaBLL.SolicitarRevisionSobrecalificacion(ObtenerSolicitante(), solicitud); return Ok("La solicitud de revisión fue enviada al PM responsable."); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible enviar la solicitud de revisión."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
