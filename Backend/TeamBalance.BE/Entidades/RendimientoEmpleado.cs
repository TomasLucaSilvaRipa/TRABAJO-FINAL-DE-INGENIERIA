namespace TeamBalance.BE.Entidades;

public class PuntoRendimientoEmpleado
{
    public DateTime Periodo { get; set; }
    public decimal CumplimientoPlazos { get; set; }
    public decimal DesvioHoras { get; set; }
    public int TareasFinalizadas { get; set; }
    public int Reasignaciones { get; set; }
    public int Observaciones { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasTrabajadas { get; set; }
}
