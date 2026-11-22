using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroProyecto
{
    public FiltroProyecto()
    {
    }

    public FiltroProyecto(int? idCliente, int? idPMResponsable, string? estado, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        Cliente = idCliente is int valorCliente ? new Cliente { ID = valorCliente } : null;
        PMResponsable = idPMResponsable is int valorPMResponsable ? new PM { ID = valorPMResponsable } : null;
        Estado = estado;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
    }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public PM? PMResponsable { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }
}
