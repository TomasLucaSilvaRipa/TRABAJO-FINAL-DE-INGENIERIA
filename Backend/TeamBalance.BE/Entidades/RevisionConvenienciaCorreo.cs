namespace TeamBalance.BE.Entidades;

public class RevisionConvenienciaCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public string NombreCliente { get; set; } = string.Empty;
    public List<string> Proyectos { get; set; } = new();
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
