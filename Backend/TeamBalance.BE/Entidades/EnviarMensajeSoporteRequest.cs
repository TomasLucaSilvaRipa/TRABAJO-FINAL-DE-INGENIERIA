using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class EnviarMensajeSoporteRequest
{
    [JsonIgnore, ReferenciaInterna]
    public ConsultaSoporte Consulta { get; set; } = new();

    public int IdConsulta
    {
        get => Consulta.ID;
        set => Consulta = Consulta.ID == value ? Consulta : new ConsultaSoporte { ID = value };
    }
    public string Mensaje { get; set; } = string.Empty;
    public string? Estado { get; set; }
}
