namespace TeamBalance.BE.Entidades;
public class ComposicionCostoOperativo
{
    public required Tarea Tarea { get; init; }
    public required Empleado Empleado { get; init; }
    public decimal HorasPlanificadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public bool CostoNoConfigurado { get; set; }
}
