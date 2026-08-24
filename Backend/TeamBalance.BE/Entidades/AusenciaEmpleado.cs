using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AusenciaEmpleado
{
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

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioResolucionNavigation { get; set; }
}
