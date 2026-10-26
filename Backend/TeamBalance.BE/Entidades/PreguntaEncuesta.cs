namespace TeamBalance.BE.Entidades;
public sealed class PreguntaEncuesta
{
    public int ID { get; set; }
    public int IdEncuesta { get; set; }
    public string EnunciadoEs { get; set; } = string.Empty;
    public string EnunciadoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<OpcionEncuesta> Opciones { get; set; } = new List<OpcionEncuesta>();
}
