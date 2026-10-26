namespace TeamBalance.BE.Entidades;
public class FiltroConvenienciaOperativa
{
    public int? IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string? EstadoProyecto { get; set; }
    public string? NivelDesvioCosto { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
