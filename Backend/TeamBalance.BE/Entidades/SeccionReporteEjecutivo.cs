namespace TeamBalance.BE.Entidades;

public class SeccionReporteEjecutivo
{
    public SeccionReporteEjecutivo()
    {
        Indicadores = new List<IndicadorReporteEjecutivo>();
    }

    public SeccionReporteEjecutivo(string titulo, string descripcion, int orden, List<IndicadorReporteEjecutivo> indicadores)
    {
        Titulo = titulo;
        Descripcion = descripcion;
        Orden = orden;
        Indicadores = indicadores;
    }

    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public int Orden { get; set; }

    public List<IndicadorReporteEjecutivo> Indicadores { get; set; }
}
