using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class OpcionEncuesta
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public PreguntaEncuesta Pregunta { get; set; } = new();

    public int IdPreguntaEncuesta
    {
        get => Pregunta.ID;
        set => Pregunta = Pregunta.ID == value ? Pregunta : new() { ID = value };
    }
    public string TextoEs { get; set; } = string.Empty;
    public string TextoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
}
