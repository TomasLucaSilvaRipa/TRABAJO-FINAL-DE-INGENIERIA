using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class EventoCalendarioEquipo
{
    public int IdEmpleado { get; set; }
    public int IdUsuario { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public decimal HorasSemanales { get; set; }
    public int? IdAusencia { get; set; }
    public string? TipoPeriodo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? HorasNoDisponibles { get; set; }
    public string? Estado { get; set; }
    public string? Motivo { get; set; }
    public string? MotivoResolucion { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public string? TareasAfectadas { get; set; }
}
