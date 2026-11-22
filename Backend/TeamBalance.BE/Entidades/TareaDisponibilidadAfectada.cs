using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class TareaDisponibilidadAfectada
{
    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new() { Nombre = string.Empty };

    public TareaDisponibilidadAfectada()
    {
    }

    public TareaDisponibilidadAfectada(int idTarea, string titulo, string nombreProyecto, DateTime? deadline)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        NombreProyecto = nombreProyecto;
        Deadline = deadline;
    }

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty };

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value, Titulo = Tarea.Titulo, Deadline = Tarea.Deadline };
    }
    public string Titulo
    {
        get => Tarea.Titulo;
        set => Tarea.Titulo = value;
    }
    public string NombreProyecto
    {
        get => Proyecto.Nombre;
        set => Proyecto.Nombre = value;
    }
    public DateTime? Deadline
    {
        get => Tarea.Deadline;
        set => Tarea.Deadline = value;
    }
}
