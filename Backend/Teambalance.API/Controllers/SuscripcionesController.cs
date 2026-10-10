using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/suscripciones")]
public sealed class SuscripcionesController : ControllerBase
{
    private readonly BLLSuscripcion _suscripcionBLL;
    private readonly BLLCuentaCorriente _cuentaCorrienteBLL;
    private readonly BLLUsuario _usuarioBLL;

    public SuscripcionesController(BLLSuscripcion suscripcionBLL, BLLCuentaCorriente cuentaCorrienteBLL, BLLUsuario usuarioBLL)
    {
        _suscripcionBLL = suscripcionBLL;
        _cuentaCorrienteBLL = cuentaCorrienteBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("actual")]
    public IActionResult ConsultarActual() => Ejecutar(() => Ok(_suscripcionBLL.ConsultarSuscripcion(ObtenerSolicitante())));

    [HttpGet("planes")]
    public IActionResult ConsultarPlanes() => Ejecutar(() => Ok(_suscripcionBLL.ConsultarPlanesDisponibles(ObtenerSolicitante())));

    [HttpGet("historial")]
    public IActionResult ConsultarHistorial() => Ejecutar(() => Ok(_suscripcionBLL.ConsultarHistorial(ObtenerSolicitante())));

    [HttpGet("cuenta-corriente")]
    public IActionResult ConsultarCuentaCorriente() => Ejecutar(() => Ok(_cuentaCorrienteBLL.Consultar(ObtenerSolicitante())));

    [HttpPost("cancelar-servicio")]
    public IActionResult CancelarServicio([FromBody] DocumentoComercial documento)
    {
        try { return Ok(_cuentaCorrienteBLL.CancelarSuscripcion(documento, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("reactivar-con-nota-credito")]
    public IActionResult ReactivarConNotaCredito([FromBody] DocumentoComercial documento)
    {
        try { return Ok(_cuentaCorrienteBLL.ReactivarConNotaCredito(documento, ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("renovacion-automatica/configuracion")]
    public IActionResult ConsultarConfiguracionRenovacion() => Ejecutar(() => Ok(_suscripcionBLL.ObtenerConfiguracionRenovacionAutomatica(ObtenerSolicitante())));

    [HttpPost("actualizar")]
    public async Task<IActionResult> Actualizar([FromBody] CambioPlanSuscripcionRequest solicitud)
    {
        try { return Ok(await _suscripcionBLL.SolicitarActualizacion(ObtenerSolicitante(), solicitud)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(new { code = "SUBSCRIPTION_OPERATION_NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { code = "SUBSCRIPTION_OPERATION_INVALID", message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { code = "SUBSCRIPTION_OPERATION_REJECTED", message = ex.Message }); }
        catch (Exception) { return StatusCode(502, "No fue posible enviar la solicitud al proveedor de pago."); }
    }

    [HttpPost("operaciones/{ReferenciaOperacion}/verificar-pago")]
    public async Task<IActionResult> VerificarPago([FromRoute] VerificarPagoSuscripcionRequest solicitudRuta, [FromBody] VerificarPagoSuscripcionRequest solicitud)
    {
        try { solicitud.ReferenciaOperacion = solicitudRuta.ReferenciaOperacion; return Ok(await _suscripcionBLL.VerificarActualizacionPago(solicitud)); }
        catch (KeyNotFoundException ex) { return NotFound(new { code = "SUBSCRIPTION_OPERATION_NOT_FOUND", message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { code = "SUBSCRIPTION_VALIDATION", message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { code = "SUBSCRIPTION_OPERATION_REJECTED", message = ex.Message }); }
        catch (Exception) { return StatusCode(502, "No fue posible verificar la operación con el proveedor de pago."); }
    }

    [HttpPost("cancelar-renovacion")]
    public async Task<IActionResult> CancelarRenovacion([FromBody] CancelacionRenovacionRequest solicitud)
    {
        try { return Ok(await _suscripcionBLL.CancelarRenovacion(ObtenerSolicitante(), solicitud)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(502, "No fue posible actualizar la renovación con Mercado Pago."); }
    }

    [HttpPost("reactivar-renovacion")]
    public async Task<IActionResult> ReactivarRenovacion()
    {
        try { return Ok(await _suscripcionBLL.ReactivarRenovacion(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(502, "No fue posible actualizar la renovación con Mercado Pago."); }
    }

    [HttpPost("renovacion-automatica/autorizar")]
    public async Task<IActionResult> AutorizarRenovacion([FromBody] AutorizarRenovacionAutomaticaRequest solicitud)
    {
        try { return Ok(await _suscripcionBLL.AutorizarRenovacionAutomatica(ObtenerSolicitante(), solicitud)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (Exception) { return StatusCode(502, "No fue posible autorizar la renovación automática con Mercado Pago."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }

    private IActionResult Ejecutar(Func<IActionResult> accion)
    {
        try { return accion(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}

