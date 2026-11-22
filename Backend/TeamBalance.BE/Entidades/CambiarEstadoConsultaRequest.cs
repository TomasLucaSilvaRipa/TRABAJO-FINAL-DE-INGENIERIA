using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class CambiarEstadoConsultaRequest
{
    [JsonIgnore, ReferenciaInterna]
    public ConsultaSoporte Consulta { get; set; } = new();

    public int IdConsulta
    {
        get => Consulta.ID;
        set => Consulta = Consulta.ID == value ? Consulta : new() { ID = value };
    }
    public string Estado { get; set; } = string.Empty;
}
