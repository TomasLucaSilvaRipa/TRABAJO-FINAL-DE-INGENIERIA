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
    public int? IdRespaldo { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public long? TamanoBytes { get; set; }
    public bool? Verificado { get; set; }
    public int? IdUsuarioSolicitante { get; set; }
    public int? ArchivosConfiguracion { get; set; }
    public string? Mensaje { get; set; }
}
