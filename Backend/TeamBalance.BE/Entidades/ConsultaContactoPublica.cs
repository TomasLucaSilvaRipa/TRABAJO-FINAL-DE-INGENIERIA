namespace TeamBalance.BE.Entidades;

public class ConsultaContactoPublica
{
    public string Nombre { get; set; } = string.Empty;
    public string? Organizacion { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
