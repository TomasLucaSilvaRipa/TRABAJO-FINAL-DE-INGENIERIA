using System.Text.Json.Serialization;

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

    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }
    public string Estado { get; set; } = string.Empty;
    public decimal CreditoAplicado { get; set; }
    public bool AplicadaAhora { get; set; }
    public List<PagoSuscripcion> Pagos { get; set; } = new();
    public int IdOperacion { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Suscripcion Suscripcion { get; set; } = new();

    public int IdSuscripcion
    {
        get => Suscripcion.ID;
        set => Suscripcion = Suscripcion.ID == value ? Suscripcion : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanNuevo { get; set; } = new();

    public int IdPlanNuevo
    {
        get => PlanNuevo.ID;
        set => PlanNuevo = PlanNuevo.ID == value ? PlanNuevo : new() { ID = value };
    }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string ReferenciaProveedor { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
    public string DetalleProveedor { get; set; } = string.Empty;
}
