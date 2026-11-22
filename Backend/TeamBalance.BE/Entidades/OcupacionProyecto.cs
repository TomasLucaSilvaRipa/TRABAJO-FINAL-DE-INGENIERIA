namespace TeamBalance.BE.Entidades;
public class OcupacionProyecto
{
    public required Proyecto Proyecto { get; init; }
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionEmpleadoProyecto> Empleados { get; set; } = new List<OcupacionEmpleadoProyecto>();
}
