namespace TeamBalance.BE.Entidades;
public sealed class ConsultaSoporteResumen
{
    public int ID { get; set; }
    public int IdUsuario { get; set; }
    public int? IdAgencia { get; set; }
    public int? IdSuscripcion { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string NombreSolicitante { get; set; } = string.Empty;
    public string? EmailSolicitante { get; set; }
    public string? NombreAgencia { get; set; }
    public string? NombrePlan { get; set; }
    public int CantidadMensajes { get; set; }
}
