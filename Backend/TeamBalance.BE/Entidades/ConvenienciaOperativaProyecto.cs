namespace TeamBalance.BE.Entidades;
public class ConvenienciaOperativaProyecto
{
    public int IdProyecto { get; set; }
    public int IdCliente { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public decimal HorasPlanificadasSinCosto { get; set; }
    public bool CostoRealIncompleto { get; set; }
    public string EstadoConveniencia { get; set; } = string.Empty;
    public string FactorPrincipal { get; set; } = string.Empty;
}
