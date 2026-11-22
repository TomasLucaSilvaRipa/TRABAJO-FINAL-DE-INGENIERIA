namespace TeamBalance.BE.Entidades;

public class RevisionConvenienciaCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public Cliente Cliente { get; set; } = new() { Nombre = string.Empty };
    public List<Proyecto> ProyectosRelacionados { get; set; } = new();
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
