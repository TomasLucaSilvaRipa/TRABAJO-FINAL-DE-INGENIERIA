namespace TeamBalance.BE.Entidades;

public class InicioSesionResultado
{
    public InicioSesionResultado(Usuario usuario, string accessToken, DateTime fechaExpiracion)
    {
        Usuario = usuario;
        AccessToken = accessToken;
        FechaExpiracion = fechaExpiracion;
    }

    public Usuario Usuario { get; set; }
    public string AccessToken { get; set; }
    public DateTime FechaExpiracion { get; set; }
}
