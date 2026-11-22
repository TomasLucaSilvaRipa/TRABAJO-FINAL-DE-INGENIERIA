using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class RespuestaPreguntaEncuesta
{
    [JsonIgnore, ReferenciaInterna]
    public PreguntaEncuesta Pregunta { get; set; } = new();

    public int IdPreguntaEncuesta
    {
        get => Pregunta.ID;
        set => Pregunta = Pregunta.ID == value ? Pregunta : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public OpcionEncuesta Opcion { get; set; } = new();

    public int IdOpcionEncuesta
    {
        get => Opcion.ID;
        set => Opcion = Opcion.ID == value ? Opcion : new() { ID = value };
    }
}
