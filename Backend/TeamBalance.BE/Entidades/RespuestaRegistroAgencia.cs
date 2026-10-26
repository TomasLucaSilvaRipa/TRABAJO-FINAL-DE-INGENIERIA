using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaRegistroAgencia
{
    public RespuestaRegistroAgencia(string mensaje, string tokenGestion, DateTime fechaExpiracionValidacion)
    {
        Mensaje = mensaje;
        TokenGestion = tokenGestion;
        FechaExpiracionValidacion = fechaExpiracionValidacion;
    }

    public string Mensaje { get; set; }
    public string TokenGestion { get; set; }
    public DateTime FechaExpiracionValidacion { get; set; }
}
