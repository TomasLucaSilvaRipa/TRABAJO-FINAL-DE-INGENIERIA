using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaSlack
{
    public RespuestaSlack(string tipoRespuesta, string texto)
    {
        TipoRespuesta = tipoRespuesta;
        Texto = texto;
    }

    [JsonPropertyName("response_type")]
    public string TipoRespuesta { get; set; }

    [JsonPropertyName("text")]
    public string Texto { get; set; }
}
