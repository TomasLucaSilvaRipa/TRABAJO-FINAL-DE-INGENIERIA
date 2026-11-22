namespace TeamBalance.BE.Entidades;
public class DesvioHorasTarea
{
    public required Tarea Tarea { get; init; }
    public Empleado? EmpleadoResponsable { get; init; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
}
