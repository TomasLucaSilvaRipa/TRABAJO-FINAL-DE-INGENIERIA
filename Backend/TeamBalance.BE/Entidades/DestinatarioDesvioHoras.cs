namespace TeamBalance.BE.Entidades;
public class DestinatarioDesvioHoras
{
    public DestinatarioDesvioHoras()
    {
    }

    public DestinatarioDesvioHoras(int idUsuario, string nombre, string email)
    {
        IdUsuario = idUsuario;
        Nombre = nombre;
        Email = email;
    }

    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
