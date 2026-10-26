namespace TeamBalance.BE.Entidades;
public sealed class ResultadoOpcionEncuesta
{
    public int IdOpcionEncuesta { get; set; }
    public string TextoEs { get; set; } = string.Empty;
    public string TextoEn { get; set; } = string.Empty;
    public int Orden { get; set; }
    public int CantidadRespuestas { get; set; }
    public decimal Porcentaje { get; set; }
}
