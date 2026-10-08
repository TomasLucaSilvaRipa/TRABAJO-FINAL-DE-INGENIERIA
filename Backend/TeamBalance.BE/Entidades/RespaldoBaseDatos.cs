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
    public int? IdUsuarioSolicitante { get; set; }
    public DateTime FechaExpiracion { get; set; }
}

public sealed class PruebaRestauracionRespaldo
{
    public int ID { get; set; }
    public int IdRespaldo { get; set; }
    public string BaseDatosDestino { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Mensaje { get; set; }
    public int? IdUsuarioSolicitante { get; set; }
}

public sealed class RestaurarRespaldoRequest
{
    public string Confirmacion { get; set; } = string.Empty;
}
