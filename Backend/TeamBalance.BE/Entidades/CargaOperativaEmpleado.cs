namespace TeamBalance.BE.Entidades;

// Resultado transitorio: se recalcula en cada consulta y no se persiste.
public class CargaOperativaEmpleado
{
    public CargaOperativaEmpleado(Empleado empleado)
    {
        Empleado = empleado;
    }

    public Empleado Empleado { get; }
    public decimal HorasCapacidadPeriodo { get; set; }
    public decimal HorasPlanificadasPeriodo { get; set; }
    public decimal HorasRegistradasPeriodo { get; set; }
    public decimal MayorPorcentajeOcupacionDiaria { get; set; }
    public bool TieneSobrecargaDiaria { get; set; }
    public Dictionary<int, decimal> HorasPlanificadasPorTarea { get; set; } = new Dictionary<int, decimal>();
}
