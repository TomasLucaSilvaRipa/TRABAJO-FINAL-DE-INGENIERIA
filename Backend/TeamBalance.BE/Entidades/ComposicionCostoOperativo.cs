namespace TeamBalance.BE.Entidades;
public class ComposicionCostoOperativo
{
    public int IdTarea { get; set; }
    public int IdEmpleado { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public string Empleado { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public decimal CostoHora { get; set; }
    public decimal HorasPlanificadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public bool CostoNoConfigurado { get; set; }
}
