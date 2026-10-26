using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using TeamBalance.BE.Entidades;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;
[ApiController]
[Route("api/contacto")]
public class ContactoController : ControllerBase
{
    private readonly EmailService _emailService;
    public ContactoController(EmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] ConsultaContacto consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta.Nombre) || string.IsNullOrWhiteSpace(consulta.Email) || string.IsNullOrWhiteSpace(consulta.Mensaje) || !MailAddress.TryCreate(consulta.Email.Trim(), out _))
        {
            return BadRequest("Completá nombre, email y mensaje con datos válidos.");
        }

        ConsultaContactoPublica mensaje = new()
        {
            Nombre = consulta.Nombre.Trim(),
            Organizacion = consulta.Organizacion?.Trim(),
            Email = consulta.Email.Trim(),
            Mensaje = consulta.Mensaje.Trim()
        };
        bool enviado = await _emailService.EnviarCorreoContacto(mensaje);
        if (!enviado)
        {
            return StatusCode(500, "No fue posible enviar la consulta en este momento.");
        }

        return Ok(new ConsultaContactoRespuesta("Recibimos tu consulta. Te responderemos a la brevedad."));
    }
}
