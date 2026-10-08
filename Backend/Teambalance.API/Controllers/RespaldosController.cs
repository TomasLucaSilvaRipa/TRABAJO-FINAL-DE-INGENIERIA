using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/respaldos")]
public sealed class RespaldosController : ControllerBase
{
    private readonly BLLRespaldoBaseDatos _respaldoBLL;
    private readonly BLLUsuario _usuarioBLL;

    public RespaldosController(BLLRespaldoBaseDatos respaldoBLL, BLLUsuario usuarioBLL) { _respaldoBLL = respaldoBLL; _usuarioBLL = usuarioBLL; }

    [HttpGet]
    public IActionResult Consultar() => Ejecutar(() =>
    {
        var resultado = _respaldoBLL.Consultar(UsuarioActual());
        return Ok(new { respaldos = resultado.Respaldos, pruebasRestauracion = resultado.Pruebas, rpoHoras = 24, rtoHoras = 4, retencionDias = 30 });
    });

    [HttpPost]
    public async Task<IActionResult> Crear()
    {
        try { return Ok(await _respaldoBLL.CrearManual(UsuarioActual())); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("{idRespaldo:int}/restauracion-aislada")]
    public async Task<IActionResult> RestaurarEnAislado(int idRespaldo, [FromBody] RestaurarRespaldoRequest solicitud)
    {
        try { return Ok(await _respaldoBLL.RestaurarEnEntornoAislado(UsuarioActual(), idRespaldo, solicitud)); }
        catch (Exception ex) { return Error(ex); }
    }

    [HttpPost("{idRespaldo:int}/restauracion-produccion")]
    public async Task<IActionResult> RestaurarProduccion(int idRespaldo, [FromBody] RestaurarRespaldoRequest solicitud)
    {
        try
        {
            await _respaldoBLL.RestaurarProduccion(UsuarioActual(), idRespaldo, solicitud);
            return Ok(new { mensaje = "La base productiva fue recuperada desde el respaldo seleccionado. Volvé a iniciar sesión para continuar." });
        }
        catch (Exception ex) { return Error(ex); }
    }

    private Usuario UsuarioActual()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }

    private IActionResult Ejecutar(Func<IActionResult> accion) { try { return accion(); } catch (Exception ex) { return Error(ex); } }
    private IActionResult Error(Exception ex) => ex switch
    {
        UnauthorizedAccessException => Unauthorized(ex.Message),
        KeyNotFoundException => NotFound(ex.Message),
        ArgumentException or InvalidOperationException or FileNotFoundException => BadRequest(ex.Message),
        _ => StatusCode(500, "No fue posible procesar el respaldo de la base de datos.")
    };
}
