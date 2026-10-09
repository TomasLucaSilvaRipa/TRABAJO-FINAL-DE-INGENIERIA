using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/helpdesk")]
public sealed class HelpDeskController : ControllerBase
{
    private readonly BLLHelpDesk _helpDeskBLL;
    private readonly BLLConsultaPlan _consultaPlanBLL;
    private readonly BLLUsuario _usuarioBLL;

    public HelpDeskController(BLLHelpDesk helpDeskBLL, BLLConsultaPlan consultaPlanBLL, BLLUsuario usuarioBLL) {
        _helpDeskBLL = helpDeskBLL;
        _consultaPlanBLL = consultaPlanBLL;
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
        return Ejecutar(() =>
        {
            Usuario usuario = UsuarioActual();
            ConsultaPlan filtroConsultaPlan = new ConsultaPlan();
            filtroConsultaPlan.Estado = filtro.Estado;
            List<BandejaSoporteItem> bandeja = _helpDeskBLL.Bandeja(usuario, filtro).Select(consulta => new BandejaSoporteItem(consulta)).Concat(_consultaPlanBLL.ConsultarBandeja(usuario, filtroConsultaPlan).Select(consulta => new BandejaSoporteItem(consulta))).OrderBy(consulta => OrdenEstado(consulta.Estado)).ThenByDescending(consulta => consulta.FechaActualizacion ?? consulta.FechaCreacion).ToList();
            return Ok(bandeja);
        });
    }

    [HttpPost("consultas-plan/{ID:int}/respuesta")]
    public IActionResult ResponderConsultaPlan([FromRoute] ConsultaPlan consulta, [FromBody] ConsultaPlan solicitud) {
        return Ejecutar(() =>
        {
            solicitud.ID = consulta.ID;
            ConsultaPlan resultado = _consultaPlanBLL.Responder(UsuarioActual(), solicitud);
            return Ok(new BandejaSoporteItem(resultado));
        });
    }

    private static int OrdenEstado(string estado) { return estado switch { "Pendiente" => 1, "En revisión" => 2, "Respondida" => 3, _ => 4 }; }

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
