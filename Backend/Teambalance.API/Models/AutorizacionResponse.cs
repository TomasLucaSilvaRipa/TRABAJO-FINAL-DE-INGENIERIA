namespace Teambalance.API.Models;
public class AutorizacionResponse
{
    public AutorizacionResponse(UsuarioSesionResponse usuario, List<RolResponse> roles, List<PermisoResponse> permisos)
    {
        Usuario = usuario;
        Roles = roles;
        Permisos = permisos;
    }

    public UsuarioSesionResponse Usuario { get; set; }
    public List<RolResponse> Roles { get; set; }
    public List<PermisoResponse> Permisos { get; set; }
}
