using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class SolicitudRenovacionMercadoPago
{
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanComercial { get; set; } = new();

    public SolicitudRenovacionMercadoPago(string nombrePlan, string referenciaExterna, string payerEmail)
    {
        NombrePlan = nombrePlan;
        ReferenciaExterna = referenciaExterna;
        PayerEmail = payerEmail;
    }

    public string NombrePlan
    {
        get => PlanComercial.Nombre;
        set => PlanComercial.Nombre = value;
    }
    public string ReferenciaExterna { get; set; }
    public string PayerEmail { get; set; }
}
