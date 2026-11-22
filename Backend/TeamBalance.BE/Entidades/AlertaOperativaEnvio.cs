using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class AlertaOperativaEnvio
{
    [JsonIgnore, ReferenciaInterna]
    public Usuario Destinatario { get; set; } = new() { Nombre = string.Empty, Email = string.Empty };

    public int ID { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public required Proyecto Proyecto { get; init; }
    public decimal Consumo { get; set; }
    public decimal Avance { get; set; }
    public string? SlackUserId { get; set; }
}
