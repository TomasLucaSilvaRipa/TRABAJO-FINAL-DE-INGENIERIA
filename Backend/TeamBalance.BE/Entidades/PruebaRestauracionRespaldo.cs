using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class PruebaRestauracionRespaldo
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public RespaldoBaseDatos Respaldo { get; set; } = new();

    public int IdRespaldo
    {
        get => Respaldo.ID;
        set => Respaldo = Respaldo.ID == value ? Respaldo : new() { ID = value };
    }
    public string BaseDatosDestino { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario? UsuarioSolicitante { get; set; }

    public int? IdUsuarioSolicitante
    {
        get => UsuarioSolicitante?.ID;
        set => UsuarioSolicitante = value.HasValue
            ? (UsuarioSolicitante?.ID == value.Value ? UsuarioSolicitante : new() { ID = value.Value })
            : null;
    }
}
