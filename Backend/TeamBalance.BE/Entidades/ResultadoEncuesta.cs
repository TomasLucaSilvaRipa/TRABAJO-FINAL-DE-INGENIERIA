using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class ResultadoEncuesta
{
    [JsonIgnore, ReferenciaInterna]
    public Encuesta Encuesta { get; set; } = new();

    public int IdEncuesta
    {
        get => Encuesta.ID;
        set => Encuesta = Encuesta.ID == value ? Encuesta : new() { ID = value, TituloEs = Encuesta.TituloEs, TituloEn = Encuesta.TituloEn };
    }
    public string TituloEs
    {
        get => Encuesta.TituloEs;
        set => Encuesta.TituloEs = value;
    }
    public string TituloEn
    {
        get => Encuesta.TituloEn;
        set => Encuesta.TituloEn = value;
    }
    public int CantidadRespuestas { get; set; }
    public List<ResultadoPreguntaEncuesta> Preguntas { get; set; } = new List<ResultadoPreguntaEncuesta>();
}
