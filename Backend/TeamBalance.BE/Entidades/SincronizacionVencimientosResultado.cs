using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class SincronizacionVencimientosResultado
{
    public SincronizacionVencimientosResultado(int idSuscripcion, int idAgencia, string estado)
    {
        IdSuscripcion = idSuscripcion;
        IdAgencia = idAgencia;
        Estado = estado;
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
    public string Estado { get; set; } = string.Empty;
}
