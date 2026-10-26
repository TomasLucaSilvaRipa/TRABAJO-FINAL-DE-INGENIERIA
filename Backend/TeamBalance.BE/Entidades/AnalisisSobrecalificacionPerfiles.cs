namespace TeamBalance.BE.Entidades;
public class AnalisisSobrecalificacionPerfiles
{
    public decimal HorasAfectadas { get; set; }
    public decimal ImpactoOperativoEstimado { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public List<SobrecalificacionTarea> Tareas { get; set; } = new List<SobrecalificacionTarea>();
}
