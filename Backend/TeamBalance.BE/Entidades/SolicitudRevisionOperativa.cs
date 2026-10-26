namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionOperativa
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdEmpleado { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombrePM { get; set; } = string.Empty;
}
