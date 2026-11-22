using System.Text.Json.Serialization;

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

    [JsonIgnore, ReferenciaInterna]
    public Suscripcion Suscripcion { get; set; } = new();

    public int IdSuscripcion
    {
        get => Suscripcion.ID;
        set => Suscripcion = Suscripcion.ID == value ? Suscripcion : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanComercial { get; set; } = new();

    public int IdPlanComercial
    {
        get => PlanComercial.ID;
        set => PlanComercial = PlanComercial.ID == value ? PlanComercial : new() { ID = value };
    }
    public string ReferenciaRenovacionProveedor { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string ReferenciaPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
}
