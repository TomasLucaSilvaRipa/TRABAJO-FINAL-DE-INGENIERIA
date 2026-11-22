using System.Text.Json.Serialization;
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
        Tareas = new List<Tarea>();
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
    public Cliente Cliente { get; set; } = new() { Nombre = null! };

    public int IdCliente
    {
        get => Cliente.ID;
        set => Cliente = Cliente.ID == value ? Cliente : new() { ID = value, Nombre = Cliente.Nombre };
    }

    [JsonIgnore, ReferenciaInterna]
    public PM PMResponsable { get; set; } = new();

    public int IdPMResponsable
    {
        get => PMResponsable.ID;
        set => PMResponsable = PMResponsable.ID == value ? PMResponsable : new() { ID = value };
    }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? Deadline { get; set; }

    public decimal HorasEstimadasTotales { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? NombreCliente
    {
        get => Cliente.Nombre;
        set => Cliente.Nombre = value!;
    }

    public string? NombrePMResponsable { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Tarea> Tareas { get; set; } = new List<Tarea>();
}
