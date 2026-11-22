using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class GuardarNoticiaRequest
{
    [JsonIgnore, ReferenciaInterna]
    public CategoriaNoticia CategoriaNoticia { get; set; } = new();

    public int IdCategoriaNoticia
    {
        get => CategoriaNoticia.ID;
        set => CategoriaNoticia = CategoriaNoticia.ID == value ? CategoriaNoticia : new() { ID = value };
    }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
