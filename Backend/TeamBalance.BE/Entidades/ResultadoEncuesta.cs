namespace TeamBalance.BE.Entidades;
public sealed class ResultadoEncuesta
{
    public int IdEncuesta { get; set; }
    public string TituloEs { get; set; } = string.Empty;
    public string TituloEn { get; set; } = string.Empty;
    public int CantidadRespuestas { get; set; }
    public List<ResultadoPreguntaEncuesta> Preguntas { get; set; } = new List<ResultadoPreguntaEncuesta>();
}
