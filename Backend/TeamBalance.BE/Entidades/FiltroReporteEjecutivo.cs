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

    public int? IdCliente { get; set; }

    public int? IdProyecto { get; set; }

    public int? IdResponsable { get; set; }

    public string? EstadoProyecto { get; set; }

    public bool AlcanceGeneral { get; set; }
}
