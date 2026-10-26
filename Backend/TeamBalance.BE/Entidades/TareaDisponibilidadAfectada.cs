using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class TareaDisponibilidadAfectada
{
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

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
}
