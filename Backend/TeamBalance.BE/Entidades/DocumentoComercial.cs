using System.Text.Json.Serialization;

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
    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new Agencia { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Suscripcion? Suscripcion { get; set; }

    public int? IdSuscripcion
    {
        get => Suscripcion?.ID;
        set => Suscripcion = value.HasValue ? (Suscripcion?.ID == value.Value ? Suscripcion : new Suscripcion { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public DocumentoComercial? DocumentoOrigen { get; set; }

    public int? IdDocumentoOrigen
    {
        get => DocumentoOrigen?.ID;
        set => DocumentoOrigen = value.HasValue ? (DocumentoOrigen?.ID == value.Value ? DocumentoOrigen : new DocumentoComercial { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial? PlanComercial { get; set; }

    public int? IdPlanComercial
    {
        get => PlanComercial?.ID;
        set => PlanComercial = value.HasValue ? (PlanComercial?.ID == value.Value ? PlanComercial : new PlanComercial { ID = value.Value }) : null;
    }
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
