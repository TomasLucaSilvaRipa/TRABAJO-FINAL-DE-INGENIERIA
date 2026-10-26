using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class ResolucionAusenciaEmpleado
{
    public int IdAusencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EsParcial { get; set; }
    public DateTime? FechaInicioAprobada { get; set; }
    public DateTime? FechaFinAprobada { get; set; }
    public decimal? HorasNoDisponiblesAprobadas { get; set; }
    public string? MotivoResolucion { get; set; }
}
