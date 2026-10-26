namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionGestion
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string? TituloTarea { get; set; }
}
