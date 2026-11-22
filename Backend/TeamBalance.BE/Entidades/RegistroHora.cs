using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamBalance.BE.Entidades;

public partial class RegistroHora
{
    public RegistroHora()
    {
    }

    public RegistroHora(int iD, int idTarea, int idEmpleado, DateTime fecha, decimal cantidadHoras, string? descripcion, bool activo)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleado = idEmpleado;
        Fecha = fecha;
        CantidadHoras = cantidadHoras;
        Descripcion = descripcion;
        Activo = activo;
    }

    public int ID { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new();

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value };
    }

    public DateTime Fecha { get; set; }

    public decimal CantidadHoras { get; set; }

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    [NotMapped]
    public decimal HorasEstimadas { get; set; }

    [NotMapped]
    public decimal HorasRealesResultantes { get; set; }

    [NotMapped]
    public decimal PorcentajeConsumo { get; set; }

    [NotMapped]
    public bool RequiereAdvertencia { get; set; }
}
