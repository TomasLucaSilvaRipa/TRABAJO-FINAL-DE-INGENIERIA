using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class DestinatarioNewsletter
{
    [JsonIgnore]
    public Usuario Usuario { get; set; } = new() { Nombre = string.Empty, Email = string.Empty };

}
