using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroProyecto
{
    public FiltroProyecto()
    {
    }

    public FiltroProyecto(int? idCliente, int? idPMResponsable, string? estado, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        IdCliente = idCliente;
        IdPMResponsable = idPMResponsable;
        Estado = estado;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
    }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    public int? IdCliente
    {
        get => Cliente?.ID;
        set => Cliente = value.HasValue ? (Cliente?.ID == value.Value ? Cliente : new Cliente { ID = value.Value }) : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public PM? PMResponsable { get; set; }

    public int? IdPMResponsable
    {
        get => PMResponsable?.ID;
        set => PMResponsable = value.HasValue ? (PMResponsable?.ID == value.Value ? PMResponsable : new PM { ID = value.Value }) : null;
    }

    public string? Estado { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }
}
