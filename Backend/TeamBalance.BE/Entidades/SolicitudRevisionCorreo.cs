using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class SolicitudRevisionCorreo
{
    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new() { Nombre = string.Empty };

    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new() { Nombre = string.Empty };

    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public string Tipo { get; set; } = string.Empty;
    public string NombreEmpleado
    {
        get => Empleado.Nombre;
        set => Empleado.Nombre = value;
    }
    public string NombreProyecto
    {
        get => Proyecto.Nombre;
        set => Proyecto.Nombre = value;
    }
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public string? TituloTarea
    {
        get => Tarea?.Titulo;
        set => Tarea = value is null ? null : new() { Titulo = value };
    }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
