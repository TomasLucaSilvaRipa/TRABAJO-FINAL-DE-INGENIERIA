namespace Teambalance.API.Models;

public class LoginResponse
{
    public LoginResponse(string accessToken, DateTime expiresAt, UsuarioSesionResponse usuario)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
        Usuario = usuario;
    }

    public string AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UsuarioSesionResponse Usuario { get; set; }
}

public class UsuarioSesionResponse
{
    public UsuarioSesionResponse(int id, int? idAgencia, string nombre, string apellido, string email, List<RolResponse> roles, List<PermisoResponse> permisos)
    {
        Id = id;
        IdAgencia = idAgencia;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
        Roles = roles;
        Permisos = permisos;
    }

    public int Id { get; set; }
    public int? IdAgencia { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public List<RolResponse> Roles { get; set; }
    public List<PermisoResponse> Permisos { get; set; }
}

public class RolResponse
{
    public RolResponse(int id, string nombre)
    {
        Id = id;
        Nombre = nombre;
    }

    public int Id { get; set; }
    public string Nombre { get; set; }
}

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
