namespace TeamBalance.BE.Entidades;
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
