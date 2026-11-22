namespace TeamBalance.BE.Entidades;
public class AccionCorreoTarea
{
    public string TokenHash { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public Usuario Usuario { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty };

    public string Accion { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto? ProyectoRelacionado { get; set; }

    public DateTime FechaExpiracion { get; set; }
    public bool Disponible { get; set; }
}
