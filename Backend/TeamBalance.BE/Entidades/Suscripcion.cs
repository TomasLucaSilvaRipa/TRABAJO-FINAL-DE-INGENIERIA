using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Suscripcion
{
    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public int IdPlanComercial { get; set; }

    public string? ReferenciaExterna { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaAlta { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public DateTime? FechaProximaRenovacion { get; set; }

    public bool RenovacionAutomatica { get; set; }

    public decimal ImporteVigente { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Agencia IdAgenciaNavigation { get; set; } = null!;

    public virtual PlanComercial IdPlanComercialNavigation { get; set; } = null!;
}
