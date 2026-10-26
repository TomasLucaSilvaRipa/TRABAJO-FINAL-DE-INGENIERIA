using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;
public class ConsultaContacto
{
    public ConsultaContacto()
    {
    }

    public ConsultaContacto(string nombre, string? organizacion, string email, string mensaje)
    {
        Nombre = nombre;
        Organizacion = organizacion;
        Email = email;
        Mensaje = mensaje;
    }

    public string Nombre { get; set; } = string.Empty;
    public string? Organizacion { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
