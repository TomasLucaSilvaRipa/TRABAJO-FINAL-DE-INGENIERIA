namespace TeamBalance.BE.Entidades;
public sealed class CobroPlataformaConfirmado
{
    public CobroPlataformaConfirmado(Agencia agencia, decimal importe, string moneda, string? proveedor, DateTime fechaCobro)
    {
        Agencia = agencia;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        FechaCobro = fechaCobro;
    }

    public Agencia Agencia { get; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string? Proveedor { get; set; }
    public DateTime FechaCobro { get; set; }
}
