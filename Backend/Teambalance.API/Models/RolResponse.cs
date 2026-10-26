namespace Teambalance.API.Models;
public class RolResponse
{
    public RolResponse(int id, string nombre, string tipoUsuario)
    {
        Id = id;
        Nombre = nombre;
        TipoUsuario = tipoUsuario;
    }

    public int Id { get; set; }
    public string Nombre { get; set; }
    public string TipoUsuario { get; set; }
}
