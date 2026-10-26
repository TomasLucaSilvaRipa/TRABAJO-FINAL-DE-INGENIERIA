namespace TeamBalance.BE.Entidades;
public sealed class GuardarNoticiaRequest
{
    public int IdCategoriaNoticia { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
