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
        IdCliente = idCliente;
        IdProyecto = idProyecto;
        IdResponsable = idResponsable;
        EstadoProyecto = estadoProyecto;
        AlcanceGeneral = alcanceGeneral;
    }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    public int? IdCliente
    {
        get => Cliente?.ID;
        set => Cliente = value.HasValue
            ? (Cliente?.ID == value.Value ? Cliente : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue
            ? (Proyecto?.ID == value.Value ? Proyecto : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public PM? Responsable { get; set; }

    public int? IdResponsable
    {
        get => Responsable?.ID;
        set => Responsable = value.HasValue
            ? (Responsable?.ID == value.Value ? Responsable : new() { ID = value.Value })
            : null;
    }

    public string? EstadoProyecto { get; set; }

    public bool AlcanceGeneral { get; set; }
}
