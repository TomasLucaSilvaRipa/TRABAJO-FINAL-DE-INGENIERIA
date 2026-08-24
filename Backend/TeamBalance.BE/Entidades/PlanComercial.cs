using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PlanComercial
{
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

    public virtual ICollection<ContratacionServicio> ContratacionServicios { get; set; } = new List<ContratacionServicio>();

    public virtual ICollection<Suscripcion> Suscripcions { get; set; } = new List<Suscripcion>();
}
