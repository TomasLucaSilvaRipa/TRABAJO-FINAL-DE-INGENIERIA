namespace TeamBalance.BE.Entidades;
/// <summary>Movimiento de cobro confirmado utilizado sólo por el BackOffice financiero.</summary>
public sealed class CobroPlataformaConfirmado
{
    public CobroPlataformaConfirmado(int idAgencia, string agencia, decimal importe, string moneda, string? proveedor, DateTime fechaCobro)
    {
        IdAgencia = idAgencia;
        Agencia = agencia;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        FechaCobro = fechaCobro;
    }

    public int IdAgencia { get; set; }
    public string Agencia { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string? Proveedor { get; set; }
    public DateTime FechaCobro { get; set; }
}
