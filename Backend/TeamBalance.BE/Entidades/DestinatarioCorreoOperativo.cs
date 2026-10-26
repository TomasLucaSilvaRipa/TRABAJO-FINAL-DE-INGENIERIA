namespace TeamBalance.BE.Entidades;
public class DestinatarioCorreoOperativo : PreferenciaCorreoOperativo
{
    public int? IdAgencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TipoUsuario { get; set; } = string.Empty;
}
