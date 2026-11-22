namespace TeamBalance.BE.Entidades;
public class SobrecalificacionTarea
{
    public required Tarea Tarea { get; init; }
    public required Proyecto Proyecto { get; init; }
    public required Empleado Empleado { get; init; }
    public string SeniorityRequerido { get; set; } = string.Empty;
    public decimal HorasAfectadas { get; set; }
    public decimal ImpactoOperativoEstimado { get; set; }
    public string Nivel { get; set; } = string.Empty;
}
