namespace Teambalance.API.Models;
public class UsuarioSesionResponse
{
    public UsuarioSesionResponse(int id, int? idAgencia, RolResponse rol, string nombre, string apellido, string email, string? fotoPerfil, List<RolResponse> roles, List<PermisoResponse> permisos)
    {
        Id = id;
        IdAgencia = idAgencia;
        Rol = rol;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
        FotoPerfil = fotoPerfil;
        Roles = roles;
        Permisos = permisos;
    }

    public int Id { get; set; }
    public int? IdAgencia { get; set; }
    public RolResponse Rol { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public string? FotoPerfil { get; set; }
    public List<RolResponse> Roles { get; set; }
    public List<PermisoResponse> Permisos { get; set; }
}
