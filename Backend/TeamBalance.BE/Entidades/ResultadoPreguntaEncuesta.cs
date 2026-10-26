namespace TeamBalance.BE.Entidades;
public sealed class ResultadoPreguntaEncuesta
{
    public int IdPreguntaEncuesta { get; set; }
    public string EnunciadoEs { get; set; } = string.Empty;
    public string EnunciadoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<ResultadoOpcionEncuesta> Opciones { get; set; } = new List<ResultadoOpcionEncuesta>();
}
