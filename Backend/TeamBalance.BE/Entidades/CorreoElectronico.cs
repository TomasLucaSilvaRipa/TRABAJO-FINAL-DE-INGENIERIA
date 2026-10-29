namespace TeamBalance.BE.Entidades;

public class CorreoElectronico
{
    public string Asunto { get; set; } = string.Empty;
    public string ContenidoHtml { get; set; } = string.Empty;
    public string Receptor { get; set; } = string.Empty;
    public string? NombreAdjunto { get; set; }
    public string? TipoContenidoAdjunto { get; set; }
    public byte[]? ContenidoAdjunto { get; set; }
}
