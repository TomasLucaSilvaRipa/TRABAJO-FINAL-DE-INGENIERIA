using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Empleado
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public decimal CostoHora { get; set; }

    public decimal HorasDisponiblesSemanales { get; set; }

    public string Seniority { get; set; } = null!;

    public string EstadoLaboral { get; set; } = null!;

    public DateTime? FechaIngreso { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual ICollection<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    public virtual ICollection<AusenciaEmpleado> AusenciaEmpleados { get; set; } = new List<AusenciaEmpleado>();

    public virtual DisponibilidadBase? DisponibilidadBase { get; set; }

    public virtual ICollection<EmpleadoSkill> EmpleadoSkills { get; set; } = new List<EmpleadoSkill>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<RecomendacionBestFit> RecomendacionBestFits { get; set; } = new List<RecomendacionBestFit>();

    public virtual ICollection<RegistroHora> RegistroHoras { get; set; } = new List<RegistroHora>();

    public virtual ICollection<SimulacionImpacto> SimulacionImpactos { get; set; } = new List<SimulacionImpacto>();

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
