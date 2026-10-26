namespace TeamBalance.BE.Entidades;
public class FiltroOcupacionOperativa
{
    public int? IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdEmpleado { get; set; }
    public string? EstadoCarga { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
