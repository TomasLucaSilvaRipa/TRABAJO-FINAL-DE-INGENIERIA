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

    public int? IdCliente { get; set; }

    public int? IdPMResponsable { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }
}
