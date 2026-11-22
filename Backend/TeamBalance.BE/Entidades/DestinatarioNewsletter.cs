using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class DestinatarioNewsletter
{
    [JsonIgnore]
    public Usuario Usuario { get; set; } = new() { Nombre = string.Empty, Email = string.Empty };

    public string Nombre { get => Usuario.Nombre; set => Usuario.Nombre = value; }
    public string Email { get => Usuario.Email; set => Usuario.Email = value; }
}
