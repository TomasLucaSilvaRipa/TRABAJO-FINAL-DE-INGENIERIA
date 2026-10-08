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

    public HelpDeskController(BLLHelpDesk helpDeskBLL, BLLUsuario usuarioBLL) {
        _helpDeskBLL = helpDeskBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("mis-consultas")]
    public IActionResult MisConsultas()
    {
        return Ejecutar(() => Ok(_helpDeskBLL.ConsultarPropias(UsuarioActual())));
    }

    [HttpPost("consultas")]
    public IActionResult Crear([FromBody] CrearConsultaSoporteRequest solicitud)
    {
        return Ejecutar(() => Ok(_helpDeskBLL.Crear(UsuarioActual(), solicitud)));
    }

    [HttpGet("consultas/{ID:int}")]
    public IActionResult Detalle([FromRoute] ConsultaSoporteResumen consulta) {
        return Ejecutar(() =>
        {
            (ConsultaSoporteResumen Consulta, List<MensajeSoporteDetalle> Mensajes) detalle = _helpDeskBLL.ConsultarDetalle(UsuarioActual(), consulta);
            return Ok(new { consulta = detalle.Consulta, mensajes = detalle.Mensajes });
        });
    }

    [HttpPost("consultas/{ID:int}/mensajes")]
    public async Task<IActionResult> Mensaje([FromRoute] ConsultaSoporteResumen consulta, [FromBody] EnviarMensajeSoporteRequest solicitud) {
        try
        {
            solicitud.IdConsulta = consulta.ID;
            await _helpDeskBLL.EnviarMensaje(UsuarioActual(), solicitud);
            return NoContent();
        }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("consultas/{ID:int}/cerrar")]
    public IActionResult Cerrar([FromRoute] ConsultaSoporteResumen consulta) {
        return Ejecutar(() =>
        {
            _helpDeskBLL.Cerrar(UsuarioActual(), consulta);
            return NoContent();
        });
    }

    [HttpPost("consultas/{ID:int}/estado")]
    public IActionResult CambiarEstado([FromRoute] ConsultaSoporteResumen consulta, [FromBody] CambiarEstadoConsultaRequest solicitud) {
        return Ejecutar(() =>
        {
            solicitud.IdConsulta = consulta.ID;
            _helpDeskBLL.CambiarEstado(UsuarioActual(), solicitud);
            return NoContent();
        });
    }

    [HttpGet("bandeja")]
    public IActionResult Bandeja([FromQuery] ConsultaSoporteResumen filtro) {
        return Ejecutar(() => Ok(_helpDeskBLL.Bandeja(UsuarioActual(), filtro)));
    }

    private Usuario UsuarioActual()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }

    private IActionResult Ejecutar(Func<IActionResult> accion)
    {
        try { return accion(); }
        catch (Exception ex) { return Error(ex); }
    }

    private IActionResult Error(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => Unauthorized(ex.Message),
            KeyNotFoundException => NotFound(ex.Message),
            ArgumentException or InvalidOperationException => BadRequest(ex.Message),
            _ => StatusCode(500, "No fue posible procesar la consulta de soporte.")
        };
    }
}
