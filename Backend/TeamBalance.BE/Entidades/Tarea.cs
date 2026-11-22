using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamBalance.BE.Entidades;

public partial class Tarea
{
    public Tarea()
    {
        AsignacionTareas = new List<AsignacionTarea>();
    }

    public Tarea(int id, bool activo)
    {
        ID = id;
        Activo = activo;
        AsignacionTareas = new List<AsignacionTarea>();
    }

    public Tarea(int id, int idProyecto, int? idEmpleadoAsignado, int? idSkillRequerido, int idEstadoTarea, int? idTareaPredecesora, string titulo, string? descripcion, string? estado, string? prioridad, string? complejidad, DateTime? fechaInicio, DateTime? deadline, DateTime? fechaFinReal, string? seniorityRequerido, string? checklistJson, string? comentariosJson, string? archivosAdjuntosJson, bool bloqueada, string? motivoBloqueo, decimal porcentajeAvance, decimal horasEstimadas, bool activo, DateTime? fechaBaja)
    {
        ID = id;
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

    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new();

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Empleado? EmpleadoAsignado { get; set; }

    public int? IdEmpleadoAsignado
    {
        get => EmpleadoAsignado?.ID;
        set => EmpleadoAsignado = value.HasValue
            ? (EmpleadoAsignado?.ID == value.Value ? EmpleadoAsignado : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public Skill? HabilidadRequerida { get; set; }

    public int? IdSkillRequerido
    {
        get => HabilidadRequerida?.ID;
        set => HabilidadRequerida = value.HasValue
            ? (HabilidadRequerida?.ID == value.Value ? HabilidadRequerida : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public EstadoTarea EstadoRelacionado { get; set; } = new();

    public int IdEstadoTarea
    {
        get => EstadoRelacionado.ID;
        set => EstadoRelacionado = EstadoRelacionado.ID == value ? EstadoRelacionado : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Tarea? TareaPredecesora { get; set; }

    public int? IdTareaPredecesora
    {
        get => TareaPredecesora?.ID;
        set => TareaPredecesora = value.HasValue
            ? (TareaPredecesora?.ID == value.Value ? TareaPredecesora : new() { ID = value.Value })
            : null;
    }

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

    public List<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    [NotMapped]
    public List<AsignacionTarea>? Asignaciones { get; set; }
}
