namespace TeamBalance.BE.Entidades;
public class AlertaOperativaEnvio
{
    public int ID { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public required Proyecto Proyecto { get; init; }
    public decimal Consumo { get; set; }
    public decimal Avance { get; set; }
    public string NombreDestinatario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SlackUserId { get; set; }
}
