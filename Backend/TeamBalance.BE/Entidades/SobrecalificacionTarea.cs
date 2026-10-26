namespace TeamBalance.BE.Entidades;
public class SobrecalificacionTarea
{
    public int IdTarea { get; set; }
    public int IdProyecto { get; set; }
    public int IdEmpleado { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string SeniorityRequerido { get; set; } = string.Empty;
    public string SeniorityAsignado { get; set; } = string.Empty;
    public string EmpleadoAsignado { get; set; } = string.Empty;
    public decimal HorasAfectadas { get; set; }
    public decimal ImpactoOperativoEstimado { get; set; }
    public string Nivel { get; set; } = string.Empty;
}
