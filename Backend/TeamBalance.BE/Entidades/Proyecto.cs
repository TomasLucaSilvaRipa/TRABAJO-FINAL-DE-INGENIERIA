using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Proyecto
{
    public Proyecto()
    {
    }

    public Proyecto(int iD, int idAgencia, int idCliente, int idPMResponsable, string nombre, string? descripcion, DateTime? fechaInicio, DateTime? deadline, decimal horasEstimadasTotales, string estado, bool activo, DateTime fechaAlta, DateTime? fechaBaja, string? nombreCliente = null, string? nombrePMResponsable = null)
    {
        ID = iD;
        IdAgencia = idAgencia;
        IdCliente = idCliente;
        IdPMResponsable = idPMResponsable;
        Nombre = nombre;
        Descripcion = descripcion;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        HorasEstimadasTotales = horasEstimadasTotales;
        Estado = estado;
        Activo = activo;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
        NombreCliente = nombreCliente;
        NombrePMResponsable = nombrePMResponsable;
    }

    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public int IdCliente { get; set; }

    public int IdPMResponsable { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? Deadline { get; set; }

    public decimal HorasEstimadasTotales { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? NombreCliente { get; set; }

    public string? NombrePMResponsable { get; set; }
}
