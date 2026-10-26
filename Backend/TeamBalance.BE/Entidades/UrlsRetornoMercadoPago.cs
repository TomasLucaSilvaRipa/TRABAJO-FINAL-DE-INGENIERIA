using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class UrlsRetornoMercadoPago
{
    public UrlsRetornoMercadoPago(string exito, string pendiente, string fallo)
    {
        Exito = exito;
        Pendiente = pendiente;
        Fallo = fallo;
    }

    [JsonPropertyName("success")]
    public string Exito { get; set; }

    [JsonPropertyName("pending")]
    public string Pendiente { get; set; }

    [JsonPropertyName("failure")]
    public string Fallo { get; set; }
}
