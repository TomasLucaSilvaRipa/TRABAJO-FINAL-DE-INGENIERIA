using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudEstadoMercadoPago
{
    public SolicitudEstadoMercadoPago(string estado) => Estado = estado;
    [JsonPropertyName("status")]
    public string Estado { get; set; }
}
