namespace TeamBalance.BE.Entidades;
public class AccionCorreoTarea
{
    public string TokenHash { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public int IdTarea { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string TituloTarea { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool Disponible { get; set; }
}
