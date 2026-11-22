using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class RespuestaEncuesta
{
    [JsonIgnore, ReferenciaInterna]
    public Encuesta Encuesta { get; set; } = new();

    public int IdEncuesta
    {
        get => Encuesta.ID;
        set => Encuesta = Encuesta.ID == value ? Encuesta : new() { ID = value };
    }
    public string IdentificadorParticipante { get; set; } = string.Empty;
    public List<RespuestaPreguntaEncuesta> Respuestas { get; set; } = new List<RespuestaPreguntaEncuesta>();
}
