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
