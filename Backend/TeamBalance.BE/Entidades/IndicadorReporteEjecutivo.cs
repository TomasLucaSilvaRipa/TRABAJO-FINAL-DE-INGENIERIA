namespace TeamBalance.BE.Entidades;

public class IndicadorReporteEjecutivo
{
    public IndicadorReporteEjecutivo()
    {
    }

    public IndicadorReporteEjecutivo(string nombre, decimal valor, string unidad, string estadoVisual, string descripcion)
    {
        Nombre = nombre;
        Valor = valor;
        Unidad = unidad;
        EstadoVisual = estadoVisual;
        Descripcion = descripcion;
    }

    public string Nombre { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public string Unidad { get; set; } = string.Empty;

    public string EstadoVisual { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
}
