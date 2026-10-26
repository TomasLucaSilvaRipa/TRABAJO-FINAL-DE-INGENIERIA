using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaMensaje
{
    public RespuestaMensaje(string mensaje) => Mensaje = mensaje;
    public string Mensaje { get; set; }
}
