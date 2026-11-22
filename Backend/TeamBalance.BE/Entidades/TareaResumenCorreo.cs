using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class TareaResumenCorreo
{
    [JsonIgnore, ReferenciaInterna]
    public Proyecto ProyectoRelacionado { get; set; } = new() { Nombre = string.Empty };

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty, Estado = string.Empty };

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value, Titulo = Tarea.Titulo, Estado = Tarea.Estado, Prioridad = Tarea.Prioridad, Deadline = Tarea.Deadline, PorcentajeAvance = Tarea.PorcentajeAvance };
    }
    public string Titulo
    {
        get => Tarea.Titulo;
        set => Tarea.Titulo = value;
    }
    public string Proyecto
    {
        get => ProyectoRelacionado.Nombre;
        set => ProyectoRelacionado.Nombre = value;
    }
    public string Estado
    {
        get => Tarea.Estado!;
        set => Tarea.Estado = value;
    }
    public string? Prioridad
    {
        get => Tarea.Prioridad;
        set => Tarea.Prioridad = value;
    }
    public DateTime? Deadline
    {
        get => Tarea.Deadline;
        set => Tarea.Deadline = value;
    }
    public decimal PorcentajeAvance
    {
        get => Tarea.PorcentajeAvance;
        set => Tarea.PorcentajeAvance = value;
    }
    public string? TokenEnProgreso { get; set; }
    public string? TokenFinalizada { get; set; }
}
