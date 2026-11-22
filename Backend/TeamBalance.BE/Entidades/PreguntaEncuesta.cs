using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class PreguntaEncuesta
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Encuesta Encuesta { get; set; } = new();

    public int IdEncuesta
    {
        get => Encuesta.ID;
        set => Encuesta = Encuesta.ID == value ? Encuesta : new() { ID = value };
    }
    public string EnunciadoEs { get; set; } = string.Empty;
    public string EnunciadoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<OpcionEncuesta> Opciones { get; set; } = new List<OpcionEncuesta>();
}
