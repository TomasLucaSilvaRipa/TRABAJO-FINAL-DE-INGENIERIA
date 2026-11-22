using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class MensajeSoporteDetalle
{
    public MensajeSoporteDetalle(int id, int idUsuario, string nombreUsuario, bool esSoporte, string mensaje, DateTime fecha)
    {
        ID = id;
        IdUsuario = idUsuario;
        NombreUsuario = nombreUsuario;
        EsSoporte = esSoporte;
        Mensaje = mensaje;
        Fecha = fecha;
    }

    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value };
    }
    public string NombreUsuario { get; set; } = string.Empty;
    public bool EsSoporte { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
