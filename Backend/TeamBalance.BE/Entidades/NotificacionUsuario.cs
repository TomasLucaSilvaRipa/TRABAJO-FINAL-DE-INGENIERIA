using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class NotificacionUsuario
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new() { Email = string.Empty };

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value, Email = Usuario.Email };
    }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool Leida { get; set; }
    public string EmailDestinatario
    {
        get => Usuario.Email;
        set => Usuario.Email = value;
    }
}
