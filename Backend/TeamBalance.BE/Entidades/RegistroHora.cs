using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class RegistroHora
{
    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleado { get; set; }

    public DateTime Fecha { get; set; }

    public decimal CantidadHoras { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Tarea IdTareaNavigation { get; set; } = null!;
}
