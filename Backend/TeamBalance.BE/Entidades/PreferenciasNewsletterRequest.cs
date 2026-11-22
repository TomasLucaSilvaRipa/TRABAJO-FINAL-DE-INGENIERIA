using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class PreferenciasNewsletterRequest
{
    [JsonIgnore, ReferenciaInterna]
    public List<CategoriaNoticia> Categorias { get; set; } = new();

    public List<int> CategoriasIds
    {
        get => Categorias?.Select(categoria => categoria.ID).ToList()!;
        set => Categorias = value?.Select(id => new CategoriaNoticia { ID = id }).ToList()!;
    }
}
