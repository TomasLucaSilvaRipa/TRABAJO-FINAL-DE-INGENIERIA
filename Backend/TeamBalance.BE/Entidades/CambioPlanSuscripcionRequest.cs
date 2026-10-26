namespace TeamBalance.BE.Entidades;
// DTOs del CU05-003. No contienen ni reciben datos sensibles de pago.
public sealed class CambioPlanSuscripcionRequest
{
    public int IdSuscripcion { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public decimal ImportePago { get; set; }
    public string MonedaPago { get; set; } = string.Empty;
    public decimal CreditoAplicado { get; set; }
}
