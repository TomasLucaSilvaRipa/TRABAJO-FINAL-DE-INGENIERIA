using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AsignacionTarea
{
    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleado { get; set; }

    public int IdUsuarioAsignador { get; set; }

    public DateTime FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Motivo { get; set; }

    public bool Activa { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Tarea IdTareaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioAsignadorNavigation { get; set; } = null!;
}
