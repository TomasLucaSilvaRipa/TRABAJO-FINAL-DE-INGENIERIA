using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class CrearConsultaSoporteRequest
{
    [JsonIgnore, ReferenciaInterna]
    public Suscripcion? Suscripcion { get; set; }

    public int? IdSuscripcion
    {
        get => Suscripcion?.ID;
        set => Suscripcion = value.HasValue ? (Suscripcion?.ID == value.Value ? Suscripcion : new Suscripcion { ID = value.Value }) : null;
    }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
