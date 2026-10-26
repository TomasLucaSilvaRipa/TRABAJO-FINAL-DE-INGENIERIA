namespace TeamBalance.BE.Entidades;
public sealed class SolicitudRenovacionMercadoPago
{
    public SolicitudRenovacionMercadoPago(string nombrePlan, string referenciaExterna, string payerEmail)
    {
        NombrePlan = nombrePlan;
        ReferenciaExterna = referenciaExterna;
        PayerEmail = payerEmail;
    }

    public string NombrePlan { get; set; }
    public string ReferenciaExterna { get; set; }
    public string PayerEmail { get; set; }
}
