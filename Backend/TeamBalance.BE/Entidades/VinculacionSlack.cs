namespace TeamBalance.BE.Entidades;

public class VinculacionSlack
{
    public Usuario Usuario { get; set; } = new();
    public string IdUsuarioSlack { get; set; } = string.Empty;
}
