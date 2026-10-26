using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaErrorSuscripcion
{
    public RespuestaErrorSuscripcion(string codigo, string mensaje)
    {
        Codigo = codigo;
        Mensaje = mensaje;
    }

    [JsonPropertyName("code")]
    public string Codigo { get; set; }

    [JsonPropertyName("message")]
    public string Mensaje { get; set; }
}
