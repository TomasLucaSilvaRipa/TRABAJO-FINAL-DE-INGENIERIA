namespace TeamBalance.BE.Entidades;
public sealed class RenovacionProveedorPendiente
{
    public RenovacionProveedorPendiente(int idSuscripcion, int idAgencia, int idPlanComercial, string referenciaRenovacionProveedor, string proveedor)
    {
        IdSuscripcion = idSuscripcion;
        IdAgencia = idAgencia;
        IdPlanComercial = idPlanComercial;
        ReferenciaRenovacionProveedor = referenciaRenovacionProveedor;
        Proveedor = proveedor;
    }

    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaRenovacionProveedor { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string ReferenciaPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
}
