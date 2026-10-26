namespace TeamBalance.BE.Entidades;
public class FiltroDesvioHoras
{
    public int? IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int? IdEmpleado { get; set; }
    public string? EstadoTarea { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? NivelDesvio { get; set; }
}
