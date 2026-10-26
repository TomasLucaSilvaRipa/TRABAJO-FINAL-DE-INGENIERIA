namespace TeamBalance.BE.Entidades;
public class ConvenienciaOperativaCliente
{
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public bool EnRevisionOperativa { get; set; }
    public string EstadoConveniencia { get; set; } = string.Empty;
    public List<ConvenienciaOperativaProyecto> Proyectos { get; set; } = new List<ConvenienciaOperativaProyecto>();
}
