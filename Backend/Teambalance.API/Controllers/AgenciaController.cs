using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using Teambalance.API.Models;
using Teambalance.API.Services;

namespace Teambalance.API.Controllers;

[Route("api/agencias")]
public class AgenciaController : ControllerBase
{
    private readonly BLLAgencia _agenciaBLL;
    private readonly BLLUsuario _usuarioBLL;

    public AgenciaController(BLLAgencia agenciaBLL, BLLUsuario usuarioBLL)
    {
        _agenciaBLL = agenciaBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("actual")]
    public IActionResult ConsultarActual() { 
        try 
        {
            Agencia agencia = _agenciaBLL.ConsultarAgencia(ObtenerSolicitante()); 
            return Ok(agencia); 
        } 
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } 
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); } 
    }

    [HttpPut("actual")]
    public IActionResult ModificarActual([FromBody] Agencia agencia) { 
        try 
        { 
            Agencia agenciaActualizada = _agenciaBLL.ModificarAgencia(agencia, ObtenerSolicitante()); 
            return Ok(agenciaActualizada); 
        } 
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } 
        catch (ArgumentException ex) { return BadRequest(ex.Message); } 
    }

    [HttpGet("suscripcion")]
    public IActionResult ConsultarSuscripcion() { 
        try 
        { 
            Suscripcion? suscripcion = _agenciaBLL.ConsultarSuscripcionActual(ObtenerSolicitante()); 
            return Ok(suscripcion); 
        } 
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } }

    [HttpPost("{referenciaContratacion}/registro")]
    public async Task<IActionResult> RegistrarAgencia([FromRoute] string referenciaContratacion, [FromBody] RegistrarAgenciaRequest solicitud)
    {
        try
        {
            Agencia agencia = solicitud.Agencia;
            Usuario? usuario = agencia.Usuarios.FirstOrDefault();
            ContratacionServicio contratacionSericio = new ContratacionServicio(referenciaContratacion);
            if (usuario is null)
            {
                return BadRequest("No se recibieron los datos del dueño.");
            }

            ResultadoRegistroAgenciaValidacion resultado = await _agenciaBLL.RegistrarAgencia(agencia, usuario, contratacionSericio, solicitud.RecibirNewsletter, solicitud.CategoriasNewsletterIds);

            RespuestaRegistroAgencia respuesta = new RespuestaRegistroAgencia("La agencia y el usuario Dueño fueron registrados correctamente.", resultado.TokenGestion, resultado.FechaExpiracion);
            return Ok(respuesta);
        }
        catch (KeyNotFoundException ex){return NotFound(ex.Message);}
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message);}
        catch (Exception){ return StatusCode(500, "No fue posible completar el registro de la agencia.");}
    }

    [HttpPost("validar-cuenta")]
    public IActionResult ValidarCuenta([FromQuery] string token)
    {
        try
        {
            if (!_agenciaBLL.ValidarCuenta(token))
            {
                return BadRequest("El enlace de validación es inválido, ya fue utilizado o venció.");
            }

            return Ok(new RespuestaMensaje("Tu correo fue confirmado. Ya podés iniciar sesión en TeamBalance."));
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible validar la cuenta en este momento.");
        }
    }

    [HttpPost("reenvio-validacion")]
    public async Task<IActionResult> ReenviarValidacion([FromBody] Usuario usuario)
    {
        try
        {
            await _agenciaBLL.ReenviarValidacion(usuario);

            return Ok(new RespuestaMensaje("Si existe una cuenta pendiente asociada a ese email, enviamos un nuevo enlace de validación."));
        }
        catch (Exception)
        {
            return StatusCode(500, "No fue posible procesar el reenvío de validación.");
        }
    }

    [HttpPost("validacion/email")]
    public async Task<IActionResult> EnviarValidacionEmailRegistro([FromBody] TokenGestionValidacionRequest request)
    {
        try
        {
            await _agenciaBLL.EnviarValidacionEmailRegistro(request.TokenGestion ?? string.Empty);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (InvalidOperationException ex) { return StatusCode(503, ex.Message); }
    }

    [HttpPost("validacion/telegram")]
    public IActionResult PrepararValidacionTelegramRegistro([FromBody] TokenGestionValidacionRequest request)
    {
        try
        {
            return Ok(_agenciaBLL.PrepararValidacionTelegramRegistro(request.TokenGestion ?? string.Empty));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpGet("validacion/telegram")]
    public IActionResult ConsultarEstadoTelegramRegistro([FromQuery] string tokenGestion)
    {
        try { return Ok(_agenciaBLL.ConsultarEstadoTelegramRegistro(tokenGestion)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost("validacion/telegram/confirmar")]
    public IActionResult ConfirmarValidacionTelegramRegistro([FromBody] ConfirmarTelegramRegistroRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Codigo)) throw new ArgumentException("Ingresá el código de Telegram.");
            _agenciaBLL.ConfirmarValidacionTelegramRegistro(request.TokenGestion ?? string.Empty, request.Codigo);
            return Ok(new RespuestaMensaje("Tu cuenta fue validada mediante Telegram. Ya podés iniciar sesión."));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("validacion/telegram/reenviar")]
    public async Task<IActionResult> ReenviarValidacionTelegramRegistro([FromBody] TokenGestionValidacionRequest request, [FromServices] TelegramBotService telegramBot, CancellationToken cancellationToken)
    {
        try
        {
            TeamBalance.BE.Entidades.EntregaTelegram entrega = _agenciaBLL.ReenviarValidacionTelegramRegistro(request.TokenGestion ?? string.Empty);
            if (!entrega.Valida || !await telegramBot.EnviarCodigoAsync(entrega.ChatId, entrega.Codigo, "Validación de cuenta", cancellationToken)) return StatusCode(503, "No fue posible enviar el código por Telegram.");
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    private Usuario ObtenerSolicitante()
    {
        string authorization = Request.Headers.Authorization.ToString();
        string token = authorization.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);
        if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
        return usuario;
    }
}

public sealed class RegistrarAgenciaRequest
{
    public Agencia Agencia { get; set; } = new Agencia();
    public bool RecibirNewsletter { get; set; }
    public List<int> CategoriasNewsletterIds { get; set; } = new List<int>();
}

public class TokenGestionValidacionRequest
{
    public string? TokenGestion { get; set; }
}

public sealed class ConfirmarTelegramRegistroRequest : TokenGestionValidacionRequest
{
    public string? Codigo { get; set; }
}
