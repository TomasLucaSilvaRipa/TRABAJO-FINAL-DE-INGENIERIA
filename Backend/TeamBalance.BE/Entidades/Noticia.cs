namespace TeamBalance.BE.Entidades;
public sealed class Noticia
{
    public int ID { get; set; }
    public int IdCategoriaNoticia { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool Activo { get; set; }
    public bool DifusionEnviada { get; set; }
}
