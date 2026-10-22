using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using Teambalance.API.Models;
using Teambalance.API.Services;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/auth/telegram")]
public class TelegramVerificationController : ControllerBase
{
    private readonly BLLUsuario _usuarioBLL;
    private readonly BLLTelegram _telegramBLL;
    private readonly TelegramBotService _telegramBot;

    public TelegramVerificationController(BLLUsuario usuarioBLL, BLLTelegram telegramBLL, TelegramBotService telegramBot)
    {
        _usuarioBLL = usuarioBLL;
        _telegramBLL = telegramBLL;
        _telegramBot = telegramBot;
    }

    [HttpGet]
    public IActionResult ConsultarEstado()
    {
        return Ok(_telegramBLL.ConsultarEstado(ObtenerUsuarioAutenticado()));
    }

    [HttpPost("vinculacion")]
    public IActionResult PrepararVinculacion()
    {
        try
        {
            if (!_telegramBot.Configurado) return StatusCode(503, "Telegram todavía no está configurado en el servidor.");
            return Ok(_telegramBLL.PrepararVinculacion(ObtenerUsuarioAutenticado()));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("vinculacion/confirmar")]
    public IActionResult ConfirmarVinculacion([FromBody] CodigoTelegramRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá el código de seis dígitos enviado por Telegram.");
            _telegramBLL.ConfirmarVinculacion(ObtenerUsuarioAutenticado(), request.Codigo);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("vinculacion/reenviar")]
    public async Task<IActionResult> ReenviarVinculacion(CancellationToken cancellationToken)
    {
        try
        {
            EntregaTelegram entrega = _telegramBLL.ReenviarCodigoVinculacion(ObtenerUsuarioAutenticado());
            if (!entrega.Valida) return BadRequest("No hay un código de vinculación disponible para reenviar.");
            if (!await _telegramBot.EnviarCodigoAsync(entrega.ChatId, entrega.Codigo, "Vinculación de Telegram", cancellationToken)) return StatusCode(503, "No fue posible enviar el código por Telegram. Verificá que el bot no esté bloqueado.");
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("desvinculacion")]
    public async Task<IActionResult> PrepararDesvinculacion(CancellationToken cancellationToken)
    {
        try
        {
            EntregaTelegram entrega = _telegramBLL.PrepararDesvinculacion(ObtenerUsuarioAutenticado());
            if (!entrega.Valida || !await _telegramBot.EnviarCodigoAsync(entrega.ChatId, entrega.Codigo, "Desvinculación de Telegram", cancellationToken)) return StatusCode(503, "No fue posible enviar el código por Telegram.");
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("desvinculacion/confirmar")]
    public IActionResult ConfirmarDesvinculacion([FromBody] CodigoTelegramRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá el código enviado por Telegram.");
            _telegramBLL.ConfirmarDesvinculacion(ObtenerUsuarioAutenticado(), request.Codigo);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    private Usuario ObtenerUsuarioAutenticado()
    {
        string? authorization = Request.Headers.Authorization;
        string token = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
