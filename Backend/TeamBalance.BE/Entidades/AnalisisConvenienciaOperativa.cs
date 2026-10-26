namespace TeamBalance.BE.Entidades;
public class AnalisisConvenienciaOperativa
{
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public int CantidadClientesEnRevision { get; set; }
    public List<ConvenienciaOperativaCliente> Clientes { get; set; } = new List<ConvenienciaOperativaCliente>();
    public List<ConvenienciaOperativaProyecto> Proyectos { get; set; } = new List<ConvenienciaOperativaProyecto>();
}
