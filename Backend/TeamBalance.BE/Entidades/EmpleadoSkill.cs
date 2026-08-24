using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class EmpleadoSkill
{
    public int ID { get; set; }

    public int IdEmpleado { get; set; }

    public int IdSkill { get; set; }

    public string? Nivel { get; set; }

    public bool Activo { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Skill IdSkillNavigation { get; set; } = null!;
}
