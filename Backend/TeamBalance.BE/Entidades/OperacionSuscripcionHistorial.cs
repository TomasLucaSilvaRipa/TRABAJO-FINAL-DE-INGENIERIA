namespace TeamBalance.BE.Entidades;
public sealed class OperacionSuscripcionHistorial
{
    public OperacionSuscripcionHistorial(int id, string tipoOperacion, string? modalidad, decimal? importe, string? moneda, string? proveedor, string? referencia, string estado, string? detalle, DateTime fecha)
    {
        ID = id;
        TipoOperacion = tipoOperacion;
        Modalidad = modalidad;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        Referencia = referencia;
        Estado = estado;
        Detalle = detalle;
        Fecha = fecha;
    }

    public int ID { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public string? Modalidad { get; set; }
    public decimal? Importe { get; set; }
    public string? Moneda { get; set; }
    public string? Proveedor { get; set; }
    public string? Referencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}
