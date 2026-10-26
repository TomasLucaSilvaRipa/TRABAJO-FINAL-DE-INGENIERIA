namespace TeamBalance.BE.Entidades;

public class EnvioCorreoOperativo
{
    public string Clave { get; set; } = string.Empty;
    public DestinatarioCorreoOperativo Destinatario { get; set; } = new();
    public string Tipo { get; set; } = string.Empty;
    public bool Enviado { get; set; }
    public string? DetalleError { get; set; }
}
