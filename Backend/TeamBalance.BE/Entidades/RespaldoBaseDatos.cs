using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class RespaldoBaseDatos
{
    public int ID { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Verificado { get; set; }
    public long? TamanoBytes { get; set; }
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
    public DateTime FechaExpiracion { get; set; }
}
