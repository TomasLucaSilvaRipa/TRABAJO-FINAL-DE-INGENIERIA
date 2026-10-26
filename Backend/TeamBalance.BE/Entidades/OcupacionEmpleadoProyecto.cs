namespace TeamBalance.BE.Entidades;
public class OcupacionEmpleadoProyecto
{
    public int IdEmpleado { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeSobreCargaProyecto { get; set; }
}
