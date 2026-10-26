namespace TeamBalance.BE.Entidades;
public sealed class OpcionEncuesta
{
    public int ID { get; set; }
    public int IdPreguntaEncuesta { get; set; }
    public string TextoEs { get; set; } = string.Empty;
    public string TextoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
}
