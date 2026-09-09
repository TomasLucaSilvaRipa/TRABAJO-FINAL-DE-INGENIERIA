using System;
using System.Collections.Generic;

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

    public int IdEmpleado { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public decimal HorasSemanales { get; set; }

    public string? Observacion { get; set; }

    public bool Activo { get; set; }
}
