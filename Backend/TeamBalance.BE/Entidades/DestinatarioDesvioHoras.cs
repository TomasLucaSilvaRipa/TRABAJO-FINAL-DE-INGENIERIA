using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class DestinatarioDesvioHoras
{
    public DestinatarioDesvioHoras()
    {
    }

    public DestinatarioDesvioHoras(int idUsuario, string nombre, string email)
    {
        Usuario = new Usuario(idUsuario) { Nombre = nombre, Email = email };
    }

    [JsonIgnore]
    public Usuario Usuario { get; set; } = new() { Nombre = string.Empty, Email = string.Empty };

}
