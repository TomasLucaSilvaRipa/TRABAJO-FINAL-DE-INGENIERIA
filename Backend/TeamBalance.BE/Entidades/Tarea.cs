using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Tarea
{
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

    public virtual ICollection<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    public virtual Empleado? IdEmpleadoAsignadoNavigation { get; set; }

    public virtual EstadoTarea IdEstadoTareaNavigation { get; set; } = null!;

    public virtual Proyecto IdProyectoNavigation { get; set; } = null!;

    public virtual Skill? IdSkillRequeridoNavigation { get; set; }

    public virtual Tarea? IdTareaPredecesoraNavigation { get; set; }

    public virtual ICollection<Tarea> InverseIdTareaPredecesoraNavigation { get; set; } = new List<Tarea>();

    public virtual ICollection<RecomendacionBestFit> RecomendacionBestFits { get; set; } = new List<RecomendacionBestFit>();

    public virtual ICollection<RegistroHora> RegistroHoras { get; set; } = new List<RegistroHora>();

    public virtual ICollection<SimulacionImpacto> SimulacionImpactos { get; set; } = new List<SimulacionImpacto>();
}
