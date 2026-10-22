using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/notificaciones")]
public class NotificacionesController : ControllerBase
{
    private readonly BLLNotificacion _notificacionBLL;
    private readonly BLLUsuario _usuarioBLL;
    public NotificacionesController(BLLNotificacion notificacionBLL, BLLUsuario usuarioBLL) { _notificacionBLL = notificacionBLL; _usuarioBLL = usuarioBLL; }

    [HttpGet]
    public IActionResult Consultar() { try { return Ok(_notificacionBLL.ConsultarPropias(ObtenerSolicitante())); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpPatch("{idNotificacion:int}/leida")]
    public IActionResult MarcarLeida(int idNotificacion) { try { _notificacionBLL.MarcarLeida(ObtenerSolicitante(), idNotificacion); return NoContent(); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }
    [HttpPatch("leer-todas")]
    public IActionResult MarcarTodasLeidas() { try { _notificacionBLL.MarcarTodasLeidas(ObtenerSolicitante()); return NoContent(); } catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
