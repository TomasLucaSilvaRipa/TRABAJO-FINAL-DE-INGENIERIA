namespace TeamBalance.BE.Entidades;

public class NotificacionUsuario
{
    public int ID { get; set; }
    public int IdUsuario { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool Leida { get; set; }
}
