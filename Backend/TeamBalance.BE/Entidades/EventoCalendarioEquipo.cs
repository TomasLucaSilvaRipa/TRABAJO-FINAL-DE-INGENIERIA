using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class EventoCalendarioEquipo
{
    [JsonIgnore]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new Empleado { ID = value };
    }
    [JsonIgnore]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new Usuario { ID = value };
    }
    public string NombreEmpleado { get; set; } = string.Empty;
    public decimal HorasSemanales { get; set; }
    [JsonIgnore]
    public AusenciaEmpleado? Ausencia { get; set; }

    public int? IdAusencia => Ausencia?.ID;
    public string? TipoPeriodo => Ausencia?.TipoPeriodo;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? HorasNoDisponibles { get; set; }
    public string? Estado => Ausencia?.Estado;
    public string? Motivo => Ausencia?.Motivo;
    public string? MotivoResolucion => Ausencia?.MotivoResolucion;
    public int CantidadTareasAfectadas { get; set; }
    public string? TareasAfectadas { get; set; }
}
