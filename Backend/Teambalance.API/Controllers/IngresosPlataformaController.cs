using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/ingresos-plataforma")]
public sealed class IngresosPlataformaController : ControllerBase
{
    private readonly BLLIngresosPlataforma _ingresosBLL;
    private readonly BLLUsuario _usuarioBLL;

    public IngresosPlataformaController(BLLIngresosPlataforma ingresosBLL, BLLUsuario usuarioBLL)
    {
        _ingresosBLL = ingresosBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("resumen")]
    public IActionResult ConsultarResumen()
    {
        try { return Ok(_ingresosBLL.ConsultarResumen(ObtenerSolicitante())); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception) { return StatusCode(500, "No fue posible consultar los ingresos de la plataforma."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
