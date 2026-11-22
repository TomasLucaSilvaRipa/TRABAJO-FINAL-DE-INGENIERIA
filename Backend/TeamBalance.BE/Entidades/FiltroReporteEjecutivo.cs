using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroReporteEjecutivo
{
    public FiltroReporteEjecutivo()
    {
    }

    public FiltroReporteEjecutivo(DateTime? fechaDesde, DateTime? fechaHasta, int? idCliente, int? idProyecto, int? idResponsable, string? estadoProyecto, bool alcanceGeneral)
    {
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        Cliente = idCliente is int valorCliente ? new Cliente { ID = valorCliente } : null;
        Proyecto = idProyecto is int valorProyecto ? new Proyecto { ID = valorProyecto } : null;
        Responsable = idResponsable is int valorResponsable ? new PM { ID = valorResponsable } : null;
        EstadoProyecto = estadoProyecto;
        AlcanceGeneral = alcanceGeneral;
    }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public PM? Responsable { get; set; }

    public string? EstadoProyecto { get; set; }

    public bool AlcanceGeneral { get; set; }
}
