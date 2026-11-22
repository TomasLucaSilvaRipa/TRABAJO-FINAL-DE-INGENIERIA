using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class ResultadoPreguntaEncuesta
{
    [JsonIgnore, ReferenciaInterna]
    public PreguntaEncuesta Pregunta { get; set; } = new();

    public int IdPreguntaEncuesta
    {
        get => Pregunta.ID;
        set => Pregunta = Pregunta.ID == value ? Pregunta : new() { ID = value, EnunciadoEs = Pregunta.EnunciadoEs, EnunciadoEn = Pregunta.EnunciadoEn, Orden = Pregunta.Orden };
    }
    public string EnunciadoEs
    {
        get => Pregunta.EnunciadoEs;
        set => Pregunta.EnunciadoEs = value;
    }
    public string EnunciadoEn
    {
        get => Pregunta.EnunciadoEn;
        set => Pregunta.EnunciadoEn = value;
    }
    public int Orden
    {
        get => Pregunta.Orden;
        set => Pregunta.Orden = value;
    }
    public List<ResultadoOpcionEncuesta> Opciones { get; set; } = new List<ResultadoOpcionEncuesta>();
}
