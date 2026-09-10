using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Empleado : Usuario
{
    public Empleado()
    {
        AsignacionTareas = new List<AsignacionTarea>();
        AusenciaEmpleados = new List<AusenciaEmpleado>();
        EmpleadoSkills = new List<EmpleadoSkill>();
        Skills = new List<Skill>();
        Tareas = new List<Tarea>();
    }

    public Empleado(int id, string nombre, string email, bool activo, decimal costoHora, decimal horasDisponiblesSemanales, string seniority, string estadoLaboral, DateTime? fechaIngreso, List<Skill> skills)
    {
        ID = id;
        Nombre = nombre;
        Email = email;
        Activo = activo;
        CostoHora = costoHora;
        HorasDisponiblesSemanales = horasDisponiblesSemanales;
        Seniority = seniority;
        EstadoLaboral = estadoLaboral;
        FechaIngreso = fechaIngreso;
        AsignacionTareas = new List<AsignacionTarea>();
        AusenciaEmpleados = new List<AusenciaEmpleado>();
        EmpleadoSkills = new List<EmpleadoSkill>();
        Skills = skills;
        Tareas = new List<Tarea>();
    }

    public decimal CostoHora { get; set; }

    public decimal HorasDisponiblesSemanales { get; set; }

    public string Seniority { get; set; } = null!;

    public string EstadoLaboral { get; set; } = null!;

    public DateTime? FechaIngreso { get; set; }


    public List<AsignacionTarea> AsignacionTareas { get; set; }

    public List<AusenciaEmpleado> AusenciaEmpleados { get; set; }

    public List<EmpleadoSkill> EmpleadoSkills { get; set; }

    public DisponibilidadBase? DisponibilidadBase { get; set; }

    public List<Skill> Skills { get; set; }

    public List<Tarea> Tareas { get; set; }
}
