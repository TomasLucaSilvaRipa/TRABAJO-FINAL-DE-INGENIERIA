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

    public int IdTarea { get; set; }

    public int IdEmpleado { get; set; }

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
