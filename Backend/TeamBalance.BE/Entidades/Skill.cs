using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Skill
{
    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Categoria { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<EmpleadoSkill> EmpleadoSkills { get; set; } = new List<EmpleadoSkill>();

    public virtual ICollection<PlantillaTarea> PlantillaTareas { get; set; } = new List<PlantillaTarea>();

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
