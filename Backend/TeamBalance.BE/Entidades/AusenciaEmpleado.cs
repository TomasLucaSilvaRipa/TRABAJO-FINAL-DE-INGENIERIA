using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AusenciaEmpleado
{
    public AusenciaEmpleado()
    {
    }

    public AusenciaEmpleado(int iD, int idEmpleado, string tipoPeriodo, DateTime fechaInicioSolicitada, DateTime fechaFinSolicitada, DateTime? fechaInicioAprobada, DateTime? fechaFinAprobada, decimal? horasNoDisponiblesSolicitadas, decimal? horasNoDisponiblesAprobadas, string? motivo, int? idUsuarioResolucion, string estado, DateTime fechaSolicitud, DateTime? fechaResolucion, string? motivoResolucion, bool activo)
    {
        ID = iD;
        IdEmpleado = idEmpleado;
        TipoPeriodo = tipoPeriodo;
        FechaInicioSolicitada = fechaInicioSolicitada;
        FechaFinSolicitada = fechaFinSolicitada;
        FechaInicioAprobada = fechaInicioAprobada;
        FechaFinAprobada = fechaFinAprobada;
        HorasNoDisponiblesSolicitadas = horasNoDisponiblesSolicitadas;
        HorasNoDisponiblesAprobadas = horasNoDisponiblesAprobadas;
        Motivo = motivo;
        IdUsuarioResolucion = idUsuarioResolucion;
        Estado = estado;
        FechaSolicitud = fechaSolicitud;
        FechaResolucion = fechaResolucion;
        MotivoResolucion = motivoResolucion;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdEmpleado { get; set; }

    public string TipoPeriodo { get; set; } = null!;

    public DateTime FechaInicioSolicitada { get; set; }

    public DateTime FechaFinSolicitada { get; set; }

    public DateTime? FechaInicioAprobada { get; set; }

    public DateTime? FechaFinAprobada { get; set; }

    public decimal? HorasNoDisponiblesSolicitadas { get; set; }

    public decimal? HorasNoDisponiblesAprobadas { get; set; }

    public string? Motivo { get; set; }

    public int? IdUsuarioResolucion { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public string? MotivoResolucion { get; set; }

    public bool Activo { get; set; }
}
