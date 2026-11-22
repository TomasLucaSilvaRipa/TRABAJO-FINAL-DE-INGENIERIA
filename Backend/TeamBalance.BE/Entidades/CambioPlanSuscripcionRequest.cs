namespace TeamBalance.BE.Entidades;

public sealed class CambioPlanSuscripcionRequest
{
    public int IdSuscripcion { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public decimal ImportePago { get; set; }
    public string MonedaPago { get; set; } = string.Empty;
    public decimal CreditoAplicado { get; set; }
    public decimal? ImporteCredito { get; set; }
    public List<DistribucionPagoSuscripcion>? Pagos { get; set; }
    public bool UsarSaldoAFavor { get; set; }
}
