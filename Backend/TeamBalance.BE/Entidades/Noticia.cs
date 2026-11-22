using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class Noticia
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public CategoriaNoticia CategoriaNoticia { get; set; } = new() { Nombre = string.Empty };

    public int IdCategoriaNoticia
    {
        get => CategoriaNoticia.ID;
        set => CategoriaNoticia = CategoriaNoticia.ID == value ? CategoriaNoticia : new() { ID = value, Nombre = CategoriaNoticia.Nombre };
    }
    public string Categoria
    {
        get => CategoriaNoticia.Nombre;
        set => CategoriaNoticia.Nombre = value;
    }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool Activo { get; set; }
    public bool DifusionEnviada { get; set; }
}
