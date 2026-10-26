using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;
public class ConsultaContactoRespuesta
{
    public ConsultaContactoRespuesta(string mensaje)
    {
        Mensaje = mensaje;
    }

    public string Mensaje { get; set; }
}
