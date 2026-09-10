using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/contacto")]
public class ContactoController : ControllerBase
{
    private readonly EmailService _emailService;
    public ContactoController(EmailService emailService) { _emailService = emailService; }

    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] ConsultaContacto consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta.Nombre) || string.IsNullOrWhiteSpace(consulta.Email) || string.IsNullOrWhiteSpace(consulta.Mensaje) || !MailAddress.TryCreate(consulta.Email.Trim(), out _)) { return BadRequest("Completá nombre, email y mensaje con datos válidos."); }
        bool enviado = await _emailService.EnviarCorreoContacto(consulta.Nombre.Trim(), consulta.Organizacion?.Trim(), consulta.Email.Trim(), consulta.Mensaje.Trim());
        if (!enviado) { return StatusCode(500, "No fue posible enviar la consulta en este momento."); }
        return Ok(new ConsultaContactoRespuesta("Recibimos tu consulta. Te responderemos a la brevedad."));
    }
}

public class ConsultaContacto
{
    public ConsultaContacto() { }
    public ConsultaContacto(string nombre, string? organizacion, string email, string mensaje) { Nombre = nombre; Organizacion = organizacion; Email = email; Mensaje = mensaje; }
    public string Nombre { get; set; } = string.Empty;
    public string? Organizacion { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public class ConsultaContactoRespuesta
{
    public ConsultaContactoRespuesta(string mensaje) { Mensaje = mensaje; }
    public string Mensaje { get; set; }
}
