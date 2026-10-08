using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/novedades")]
public sealed class NovedadesController : ControllerBase
{
    private readonly BLLNovedades _novedadesBLL;
    private readonly BLLUsuario _usuarioBLL;
    public NovedadesController(BLLNovedades novedadesBLL, BLLUsuario usuarioBLL) { _novedadesBLL = novedadesBLL; _usuarioBLL = usuarioBLL; }
    [HttpGet("publicas")] public IActionResult Publicas() => Ok(_novedadesBLL.Publicas());
    [HttpGet("categorias")] public IActionResult Categorias() => Ok(_novedadesBLL.Categorias());
    [HttpGet("preferencias")] public IActionResult Preferencias() => Ejecutar(() => Ok(_novedadesBLL.Preferencias(UsuarioActual())));
    [HttpPut("preferencias")] public IActionResult GuardarPreferencias([FromBody] PreferenciasNewsletterRequest solicitud) => Ejecutar(() => { _novedadesBLL.GuardarPreferencias(UsuarioActual(), solicitud); return NoContent(); });
    [HttpGet("gestion")] public IActionResult Gestion() => Ejecutar(() => Ok(_novedadesBLL.Gestion(UsuarioActual())));
    [HttpPost] public async Task<IActionResult> Guardar([FromBody] GuardarNoticiaRequest solicitud) { try { return Ok(await _novedadesBLL.Guardar(UsuarioActual(), solicitud)); } catch (Exception ex) { return Error(ex); } }
    [HttpPost("{idNoticia:int}/baja")] public IActionResult Bajar(int idNoticia) => Ejecutar(() => { _novedadesBLL.Bajar(UsuarioActual(), idNoticia); return NoContent(); });
    private Usuario UsuarioActual() { string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
    private IActionResult Ejecutar(Func<IActionResult> accion) { try { return accion(); } catch (Exception ex) { return Error(ex); } }
    private IActionResult Error(Exception ex) => ex switch { UnauthorizedAccessException => Unauthorized(ex.Message), KeyNotFoundException => NotFound(ex.Message), ArgumentException or InvalidOperationException => BadRequest(ex.Message), _ => StatusCode(500, "No fue posible procesar las novedades.") };
}
