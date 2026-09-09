using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Suscripcion
{
    public Suscripcion()
    {
    }

    public Suscripcion(int iD, int idAgencia, int idPlanComercial, string? referenciaExterna, string estado, DateTime fechaAlta, DateTime fechaVencimiento, DateTime? fechaProximaRenovacion, bool renovacionAutomatica, decimal importeVigente, bool activo, DateTime? fechaBaja)
    {
        ID = iD;
        IdAgencia = idAgencia;
        IdPlanComercial = idPlanComercial;
        ReferenciaExterna = referenciaExterna;
        Estado = estado;
        FechaAlta = fechaAlta;
        FechaVencimiento = fechaVencimiento;
        FechaProximaRenovacion = fechaProximaRenovacion;
        RenovacionAutomatica = renovacionAutomatica;
        ImporteVigente = importeVigente;
        Activo = activo;
        FechaBaja = fechaBaja;
    }

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
}
