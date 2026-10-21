using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AsignacionTarea
{
    public AsignacionTarea()
    {
    }

    public AsignacionTarea(int iD, int idTarea, int idEmpleado, int idUsuarioAsignador, DateTime fechaDesde, DateTime? fechaHasta, string? motivo, decimal horasAsignadas, bool activa)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleado = idEmpleado;
        IdUsuarioAsignador = idUsuarioAsignador;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        Motivo = motivo;
        HorasAsignadas = horasAsignadas;
        Activa = activa;
    }

    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleado { get; set; }

    public int IdUsuarioAsignador { get; set; }

    public DateTime FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Motivo { get; set; }

    // Horas de la tarea que forman parte de la carga planificada de este empleado.
    public decimal HorasAsignadas { get; set; }

    public bool Activa { get; set; }

    // Navegaciones del modelo de persistencia. No crean una entidad nueva ni un
    // nuevo tipo de persona: la asignación siempre apunta a un Empleado existente.
    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Tarea? IdTareaNavigation { get; set; }

    public virtual Usuario? IdUsuarioAsignadorNavigation { get; set; }
}
