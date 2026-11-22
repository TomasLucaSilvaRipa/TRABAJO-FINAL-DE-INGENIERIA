using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public partial class DisponibilidadBase
{
    public DisponibilidadBase()
    {
    }

    public DisponibilidadBase(int iD, int idEmpleado, TimeOnly horaInicio, TimeOnly horaFin, decimal horasSemanales, string? observacion, bool activo)
    {
        ID = iD;
        IdEmpleado = idEmpleado;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        HorasSemanales = horasSemanales;
        Observacion = observacion;
        Activo = activo;
    }

    public int ID { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new Empleado { ID = value };
    }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public decimal HorasSemanales { get; set; }

    public string? Observacion { get; set; }

    public bool Activo { get; set; }
}
