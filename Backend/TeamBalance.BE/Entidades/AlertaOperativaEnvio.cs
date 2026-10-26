namespace TeamBalance.BE.Entidades;
public class AlertaOperativaEnvio
{
    public int ID { get; set; }
    public int IdProyecto { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public decimal Consumo { get; set; }
    public decimal Avance { get; set; }
    public string NombreDestinatario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SlackUserId { get; set; }
}
