namespace TeamBalance.BE.Entidades;
public class AnalisisOcupacionOperativa
{
    public decimal PorcentajeOcupacionGeneral { get; set; }
    public string EstadoCargaGeneral { get; set; } = string.Empty;
    public decimal HorasDisponiblesSemanales { get; set; }
    public decimal HorasEstimadasActivas { get; set; }
    public int CantidadSubutilizados { get; set; }
    public int CantidadBalanceados { get; set; }
    public int CantidadSobrecargados { get; set; }
    public List<OcupacionEmpleado> Empleados { get; set; } = new List<OcupacionEmpleado>();
    public List<OcupacionProyecto> Proyectos { get; set; } = new List<OcupacionProyecto>();
    public List<OcupacionCliente> Clientes { get; set; } = new List<OcupacionCliente>();
}
