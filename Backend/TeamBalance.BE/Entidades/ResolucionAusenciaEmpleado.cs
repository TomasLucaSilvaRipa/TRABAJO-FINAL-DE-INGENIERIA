using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class ResolucionAusenciaEmpleado
{
    [JsonIgnore, ReferenciaInterna]
    public AusenciaEmpleado Ausencia { get; set; } = new();

    public int IdAusencia
    {
        get => Ausencia.ID;
        set => Ausencia = Ausencia.ID == value ? Ausencia : new() { ID = value };
    }
    public string Estado { get; set; } = string.Empty;
    public bool EsParcial { get; set; }
    public DateTime? FechaInicioAprobada { get; set; }
    public DateTime? FechaFinAprobada { get; set; }
    public decimal? HorasNoDisponiblesAprobadas { get; set; }
    public string? MotivoResolucion { get; set; }
}
