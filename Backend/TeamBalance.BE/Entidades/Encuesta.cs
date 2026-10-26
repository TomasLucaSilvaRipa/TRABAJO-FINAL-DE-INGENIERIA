namespace TeamBalance.BE.Entidades;
public sealed class Encuesta
{
    public int ID { get; set; }
    public string TituloEs { get; set; } = string.Empty;
    public string TituloEn { get; set; } = string.Empty;
    public string DescripcionEs { get; set; } = string.Empty;
    public string DescripcionEn { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public bool Activo { get; set; }
    public int CantidadRespuestas { get; set; }
    public List<PreguntaEncuesta> Preguntas { get; set; } = new List<PreguntaEncuesta>();
}
