using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/helpdesk")]
public sealed class HelpDeskController : ControllerBase
{
    private readonly BLLHelpDesk _helpDeskBLL;
    private readonly BLLUsuario _usuarioBLL;
    public HelpDeskController(BLLHelpDesk helpDeskBLL, BLLUsuario usuarioBLL) { _helpDeskBLL = helpDeskBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet("mis-consultas")] public IActionResult MisConsultas() => Ejecutar(() => Ok(_helpDeskBLL.ConsultarPropias(UsuarioActual())));
    [HttpPost("consultas")] public IActionResult Crear([FromBody] CrearConsultaSoporteRequest solicitud) => Ejecutar(() => Ok(_helpDeskBLL.Crear(UsuarioActual(), solicitud)));
    [HttpGet("consultas/{idConsulta:int}")] public IActionResult Detalle(int idConsulta) => Ejecutar(() => { var d = _helpDeskBLL.ConsultarDetalle(UsuarioActual(), idConsulta); return Ok(new { consulta = d.Consulta, mensajes = d.Mensajes }); });
    [HttpPost("consultas/{idConsulta:int}/mensajes")] public async Task<IActionResult> Mensaje(int idConsulta, [FromBody] EnviarMensajeSoporteRequest solicitud) { try { await _helpDeskBLL.EnviarMensaje(UsuarioActual(), idConsulta, solicitud); return NoContent(); } catch (Exception ex) { return Error(ex); } }
    [HttpPost("consultas/{idConsulta:int}/cerrar")] public IActionResult Cerrar(int idConsulta) => Ejecutar(() => { _helpDeskBLL.Cerrar(UsuarioActual(), idConsulta); return NoContent(); });
    [HttpPost("consultas/{idConsulta:int}/estado")] public IActionResult CambiarEstado(int idConsulta, [FromBody] CambiarEstadoConsultaRequest solicitud) => Ejecutar(() => { _helpDeskBLL.CambiarEstado(UsuarioActual(), idConsulta, solicitud.Estado); return NoContent(); });
    [HttpGet("bandeja")] public IActionResult Bandeja([FromQuery] string? estado) => Ejecutar(() => Ok(_helpDeskBLL.Bandeja(UsuarioActual(), estado)));
    private Usuario UsuarioActual() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
    private IActionResult Ejecutar(Func<IActionResult> accion) { try { return accion(); } catch (Exception ex) { return Error(ex); } }
    private IActionResult Error(Exception ex) => ex switch { UnauthorizedAccessException => Unauthorized(ex.Message), KeyNotFoundException => NotFound(ex.Message), ArgumentException or InvalidOperationException => BadRequest(ex.Message), _ => StatusCode(500, "No fue posible procesar la consulta de soporte.") };
}
