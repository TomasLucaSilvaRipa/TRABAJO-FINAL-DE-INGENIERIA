using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public sealed class RegistroContinuidadXml
{
    public RegistroContinuidadXml() { }

    public RegistroContinuidadXml(string idOperacion, string tipo, string estado, DateTime fechaInicioUtc, DateTime? fechaFinUtc, int? idRespaldo, string nombreArchivo, long? tamanoBytes, bool? verificado, int? idUsuarioSolicitante, int? archivosConfiguracion, string? mensaje)
    {
        IdOperacion = idOperacion;
        Tipo = tipo;
        Estado = estado;
        FechaInicioUtc = fechaInicioUtc;
        FechaFinUtc = fechaFinUtc;
        IdRespaldo = idRespaldo;
        NombreArchivo = nombreArchivo;
        TamanoBytes = tamanoBytes;
        Verificado = verificado;
        IdUsuarioSolicitante = idUsuarioSolicitante;
        ArchivosConfiguracion = archivosConfiguracion;
        Mensaje = mensaje;
    }

    public string IdOperacion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicioUtc { get; set; }
    public DateTime? FechaFinUtc { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public RespaldoBaseDatos? Respaldo { get; set; }

    public int? IdRespaldo
    {
        get => Respaldo?.ID;
        set => Respaldo = value.HasValue
            ? (Respaldo?.ID == value.Value ? Respaldo : new() { ID = value.Value })
            : null;
    }
    public string NombreArchivo { get; set; } = string.Empty;
    public long? TamanoBytes { get; set; }
    public bool? Verificado { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario? UsuarioSolicitante { get; set; }

    public int? IdUsuarioSolicitante
    {
        get => UsuarioSolicitante?.ID;
        set => UsuarioSolicitante = value.HasValue
            ? (UsuarioSolicitante?.ID == value.Value ? UsuarioSolicitante : new() { ID = value.Value })
            : null;
    }
    public int? ArchivosConfiguracion { get; set; }
    public string? Mensaje { get; set; }
}
