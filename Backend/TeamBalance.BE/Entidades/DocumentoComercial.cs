namespace TeamBalance.BE.Entidades;

public class DocumentoComercial
{
    public DocumentoComercial()
    {
    }

    public DocumentoComercial(int id, int idAgencia, int? idSuscripcion, int? idDocumentoOrigen, string tipo, string numero, string concepto, decimal importe, decimal saldoPendiente, string moneda, string estado, DateTime fechaEmision, DateTime? fechaVencimiento, string? motivo)
    {
        ID = id;
        IdAgencia = idAgencia;
        IdSuscripcion = idSuscripcion;
        IdDocumentoOrigen = idDocumentoOrigen;
        Tipo = tipo;
        Numero = numero;
        Concepto = concepto;
        Importe = importe;
        SaldoPendiente = saldoPendiente;
        Moneda = moneda;
        Estado = estado;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaVencimiento;
        Motivo = motivo;
    }

    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int? IdSuscripcion { get; set; }
    public int? IdDocumentoOrigen { get; set; }
    public int? IdPlanComercial { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Moneda { get; set; } = "ARS";
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? Motivo { get; set; }
}
