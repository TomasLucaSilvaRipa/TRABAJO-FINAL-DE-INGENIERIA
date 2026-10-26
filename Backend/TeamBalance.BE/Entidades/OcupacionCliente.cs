namespace TeamBalance.BE.Entidades;
public class OcupacionCliente
{
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionProyecto> Proyectos { get; set; } = new List<OcupacionProyecto>();
}
