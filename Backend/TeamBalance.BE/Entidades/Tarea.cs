using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Tarea
{
    public Tarea()
    {
    }

    public Tarea(int iD, int idProyecto, int? idEmpleadoAsignado, int? idSkillRequerido, int idEstadoTarea, int? idTareaPredecesora, string titulo, string? descripcion, string? estado, string? prioridad, string? complejidad, DateTime? fechaInicio, DateTime? deadline, DateTime? fechaFinReal, string? seniorityRequerido, string? checklistJson, string? comentariosJson, string? archivosAdjuntosJson, bool bloqueada, string? motivoBloqueo, decimal porcentajeAvance, decimal horasEstimadas, bool activo, DateTime? fechaBaja)
    {
        ID = iD;
        IdProyecto = idProyecto;
        IdEmpleadoAsignado = idEmpleadoAsignado;
        IdSkillRequerido = idSkillRequerido;
        IdEstadoTarea = idEstadoTarea;
        IdTareaPredecesora = idTareaPredecesora;
        Titulo = titulo;
        Descripcion = descripcion;
        Estado = estado;
        Prioridad = prioridad;
        Complejidad = complejidad;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        FechaFinReal = fechaFinReal;
        SeniorityRequerido = seniorityRequerido;
        ChecklistJson = checklistJson;
        ComentariosJson = comentariosJson;
        ArchivosAdjuntosJson = archivosAdjuntosJson;
        Bloqueada = bloqueada;
        MotivoBloqueo = motivoBloqueo;
        PorcentajeAvance = porcentajeAvance;
        HorasEstimadas = horasEstimadas;
        Activo = activo;
        FechaBaja = fechaBaja;
    }

    public int ID { get; set; }

    public int IdProyecto { get; set; }

    public int? IdEmpleadoAsignado { get; set; }

    public int? IdSkillRequerido { get; set; }

    public int IdEstadoTarea { get; set; }

    public int? IdTareaPredecesora { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Estado { get; set; }

    public string? Prioridad { get; set; }

    public string? Complejidad { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public string? SeniorityRequerido { get; set; }

    public string? ChecklistJson { get; set; }

    public string? ComentariosJson { get; set; }

    public string? ArchivosAdjuntosJson { get; set; }

    public bool Bloqueada { get; set; }

    public string? MotivoBloqueo { get; set; }

    public decimal PorcentajeAvance { get; set; }

    public decimal HorasEstimadas { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }
}
