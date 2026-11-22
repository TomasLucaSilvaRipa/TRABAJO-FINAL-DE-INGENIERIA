using System.Text.Json.Serialization;
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

    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public PlanComercial PlanComercial { get; set; } = new();

    public int IdPlanComercial
    {
        get => PlanComercial.ID;
        set => PlanComercial = PlanComercial.ID == value ? PlanComercial : new() { ID = value, Nombre = PlanComercial.Nombre, Periodicidad = PlanComercial.Periodicidad, Moneda = PlanComercial.Moneda };
    }

    public string? ReferenciaExterna { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaAlta { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public DateTime? FechaProximaRenovacion { get; set; }

    public bool RenovacionAutomatica { get; set; }

    public decimal ImporteVigente { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? ReferenciaRenovacionProveedor { get; set; }

    public string? EstadoRenovacionProveedor { get; set; }

    public string? ProveedorRenovacion { get; set; }

    public DateTime? FechaSincronizacionRenovacion { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? MotivoRenovacion { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? NombrePlan
    {
        get => PlanComercial.Nombre;
        set => PlanComercial.Nombre = value!;
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PeriodicidadPlan
    {
        get => PlanComercial.Periodicidad;
        set => PlanComercial.Periodicidad = value!;
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? Moneda
    {
        get => PlanComercial.Moneda;
        set => PlanComercial.Moneda = value!;
    }
}
