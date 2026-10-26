namespace Teambalance.API.Models;
public class PermisoResponse
{
    public PermisoResponse(int id, string? codigo, string nombre, string? url)
    {
        Id = id;
        Codigo = codigo;
        Nombre = nombre;
        Url = url;
    }

    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; }
    public string? Url { get; set; }
}
