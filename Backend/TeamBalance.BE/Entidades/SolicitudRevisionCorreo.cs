namespace TeamBalance.BE.Entidades;

public class SolicitudRevisionCorreo
{
    public Proyecto Proyecto { get; set; } = new() { Nombre = string.Empty };
    public Empleado Empleado { get; set; } = new() { Nombre = string.Empty };
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public Tarea? Tarea { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
