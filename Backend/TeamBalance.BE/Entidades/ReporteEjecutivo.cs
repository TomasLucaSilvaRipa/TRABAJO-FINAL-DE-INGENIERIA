namespace TeamBalance.BE.Entidades;

public class ReporteEjecutivo
{
    public ReporteEjecutivo()
    {
        Secciones = new List<SeccionReporteEjecutivo>();
    }

    public ReporteEjecutivo(int idReporte, string titulo, DateTime? fechaDesde, DateTime? fechaHasta, DateTime fechaGeneracion, List<SeccionReporteEjecutivo> secciones)
    {
        IdReporte = idReporte;
        Titulo = titulo;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        FechaGeneracion = fechaGeneracion;
        Secciones = secciones;
    }

    public int IdReporte { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public List<SeccionReporteEjecutivo> Secciones { get; set; }
}
