namespace TeamBalance.BE.Entidades;

public class ResultadoRiesgoRetraso
{
    public ResultadoRiesgoRetraso()
    {
        Proyectos = new List<DetalleRiesgoProyecto>();
    }

    public ResultadoRiesgoRetraso(List<DetalleRiesgoProyecto> proyectos)
    {
        Proyectos = proyectos;
    }

    public List<DetalleRiesgoProyecto> Proyectos { get; set; }
}
