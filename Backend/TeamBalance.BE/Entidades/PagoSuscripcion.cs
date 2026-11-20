namespace TeamBalance.BE.Entidades;

public sealed record CotizacionPagoSuscripcion(decimal ImportePlan, string MonedaPlan, decimal ImporteMercadoPago, string MonedaMercadoPago);

public sealed class DistribucionPagoSuscripcion
{
    public string Proveedor { get; set; } = string.Empty;
    public string MedioPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
}

public sealed class PagoSuscripcion
{
    public int ID { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string MedioPago { get; set; } = string.Empty;
    public decimal ImportePlan { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public string UrlPago { get; set; } = string.Empty;
    public string ReferenciaProveedor { get; set; } = string.Empty;
    public int Intento { get; set; }
}
