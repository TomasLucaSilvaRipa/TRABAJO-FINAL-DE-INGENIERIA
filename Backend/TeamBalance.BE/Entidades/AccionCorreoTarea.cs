namespace TeamBalance.BE.Entidades;
public class AccionCorreoTarea
{
    public string TokenHash { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public Usuario Usuario { get; set; } = new();

    // Campo del contrato de salida.
    public int IdUsuario => Usuario.ID;
    [System.Text.Json.Serialization.JsonIgnore]
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty };

    // Campo del contrato de salida.
    public int IdTarea => Tarea.ID;
    public string Accion { get; set; } = string.Empty;
    public string TituloTarea => Tarea.Titulo;
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto? ProyectoRelacionado { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("proyecto")]
    public string NombreProyecto => ProyectoRelacionado?.Nombre ?? string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool Disponible { get; set; }
}
