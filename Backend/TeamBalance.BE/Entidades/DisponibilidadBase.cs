using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class DisponibilidadBase
{
    public int ID { get; set; }

    public int IdEmpleado { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public decimal HorasSemanales { get; set; }

    public string? Observacion { get; set; }

    public bool Activo { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;
}
