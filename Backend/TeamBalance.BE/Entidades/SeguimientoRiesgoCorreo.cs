namespace TeamBalance.BE.Entidades;

public class SeguimientoRiesgoCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public Proyecto Proyecto { get; set; } = new();
    public string? TituloTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
