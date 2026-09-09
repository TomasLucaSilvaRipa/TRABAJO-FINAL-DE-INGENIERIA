using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PlanComercial
{
    public PlanComercial()
    {
    }

    public PlanComercial(int iD, string nombre, string? descripcion, string periodicidad, decimal precioVigente, string moneda, int duracionMeses, string? alcanceFuncional, string? condicionesRenovacion, bool activo, DateTime fechaVigenciaDesde, DateTime? fechaVigenciaHasta)
    {
        ID = iD;
        Nombre = nombre;
        Descripcion = descripcion;
        Periodicidad = periodicidad;
        PrecioVigente = precioVigente;
        Moneda = moneda;
        DuracionMeses = duracionMeses;
        AlcanceFuncional = alcanceFuncional;
        CondicionesRenovacion = condicionesRenovacion;
        Activo = activo;
        FechaVigenciaDesde = fechaVigenciaDesde;
        FechaVigenciaHasta = fechaVigenciaHasta;
    }

    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Periodicidad { get; set; } = null!;

    public decimal PrecioVigente { get; set; }

    public string Moneda { get; set; } = null!;

    public int DuracionMeses { get; set; }

    public string? AlcanceFuncional { get; set; }

    public string? CondicionesRenovacion { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaVigenciaDesde { get; set; }

    public DateTime? FechaVigenciaHasta { get; set; }
}
