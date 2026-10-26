namespace TeamBalance.BE.Entidades;
public sealed class InicioActualizacionSuscripcion
{
    public InicioActualizacionSuscripcion(string urlPago, string referenciaOperacion, decimal importe, string moneda, DateTime fechaAplicacion)
    {
        UrlPago = urlPago;
        ReferenciaOperacion = referenciaOperacion;
        Importe = importe;
        Moneda = moneda;
        FechaAplicacion = fechaAplicacion;
    }

    public string UrlPago { get; set; }
    public string ReferenciaOperacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
    public DateTime FechaAplicacion { get; set; }
}
