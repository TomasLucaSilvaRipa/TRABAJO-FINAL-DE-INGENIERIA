using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class SolicitudRenovacionMercadoPago
{
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanComercial { get; }

    public SolicitudRenovacionMercadoPago(PlanComercial planComercial, string referenciaExterna, string payerEmail, DateTimeOffset fechaPrimerCobro)
    {
        PlanComercial = planComercial;
        FechaPrimerCobro = fechaPrimerCobro;
        ReferenciaExterna = referenciaExterna;
        PayerEmail = payerEmail;
    }

    public DateTimeOffset FechaPrimerCobro { get; }
    public string ReferenciaExterna { get; set; }
    public string PayerEmail { get; set; }
}
