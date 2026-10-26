namespace TeamBalance.BE.Entidades;
public class OcupacionProyecto
{
    public int IdProyecto { get; set; }
    public int IdCliente { get; set; }
    public int IdPMResponsable { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionEmpleadoProyecto> Empleados { get; set; } = new List<OcupacionEmpleadoProyecto>();
}
