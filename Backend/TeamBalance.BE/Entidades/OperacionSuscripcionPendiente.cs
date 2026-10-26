namespace TeamBalance.BE.Entidades;
public sealed class OperacionSuscripcionPendiente
{
    public OperacionSuscripcionPendiente()
    {
    }

    public OperacionSuscripcionPendiente(string referenciaInterna)
    {
        ReferenciaInterna = referenciaInterna;
    }

    public OperacionSuscripcionPendiente(int idOperacion, int idSuscripcion, int idPlanNuevo, string referenciaInterna, decimal importe, string moneda, string proveedor, string tipoOperacion = "")
    {
        IdOperacion = idOperacion;
        IdSuscripcion = idSuscripcion;
        IdPlanNuevo = idPlanNuevo;
        ReferenciaInterna = referenciaInterna;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        TipoOperacion = tipoOperacion;
    }

    public int IdOperacion { get; set; }
    public int IdSuscripcion { get; set; }
    public int IdPlanNuevo { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string ReferenciaProveedor { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
    public string DetalleProveedor { get; set; } = string.Empty;
}
