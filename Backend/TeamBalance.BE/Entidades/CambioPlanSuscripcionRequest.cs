using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public sealed class CambioPlanSuscripcionRequest
{
    [JsonIgnore, ReferenciaInterna]
    public Suscripcion Suscripcion { get; set; } = new();

    public int IdSuscripcion
    {
        get => Suscripcion.ID;
        set => Suscripcion = Suscripcion.ID == value ? Suscripcion : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanComercial { get; set; } = new();

    public int IdPlanComercial
    {
        get => PlanComercial.ID;
        set => PlanComercial = PlanComercial.ID == value ? PlanComercial : new() { ID = value };
    }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public decimal ImportePago { get; set; }
    public string MonedaPago { get; set; } = string.Empty;
    public decimal CreditoAplicado { get; set; }
    public decimal? ImporteCredito { get; set; }
    public List<DistribucionPagoSuscripcion>? Pagos { get; set; }
    public bool UsarSaldoAFavor { get; set; }
}
