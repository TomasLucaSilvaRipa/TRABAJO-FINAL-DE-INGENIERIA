using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Empleado:Usuario
{
    public Empleado() { }

    public Empleado(int id, string nombre, string? email, bool activo, decimal costoHora, decimal horasDisponiblesSemanales, string seniority, string estadoLaboral, DateTime? fechaIngreso, List<Skill> _skills)
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
        skills = _skills;
    }

    public decimal CostoHora { get; set; }

    public decimal HorasDisponiblesSemanales { get; set; }

    public string Seniority { get; set; } = null!;

    public string EstadoLaboral { get; set; } = null!;

    public DateTime? FechaIngreso { get; set; }


    public  List<AsignacionTarea> AsignacionTareas { get; set; } = new List<AsignacionTarea>();

    public List<AusenciaEmpleado> AusenciaEmpleados { get; set; } = new List<AusenciaEmpleado>();

    public DisponibilidadBase? DisponibilidadBase { get; set; }

    public List<Skill> skills { get; set; }

    public virtual ICollection<RecomendacionBestFit> RecomendacionBestFits { get; set; } = new List<RecomendacionBestFit>();

    public virtual ICollection<RegistroHora> RegistroHoras { get; set; } = new List<RegistroHora>();

    public virtual ICollection<SimulacionImpacto> SimulacionImpactos { get; set; } = new List<SimulacionImpacto>();

    public List<Tarea> Tareas { get; set; }
}
