namespace TeamBalance.BE.Entidades;

public class SolicitudRevisionCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public string Tipo { get; set; } = string.Empty;
    public string NombreEmpleado { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string? TituloTarea { get; set; }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
