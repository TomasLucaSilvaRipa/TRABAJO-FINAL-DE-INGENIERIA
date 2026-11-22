namespace TeamBalance.BE.Entidades;
public class DetalleDesvioHorasTarea
{
    public required Proyecto Proyecto { get; init; }
    public required Tarea Tarea { get; init; }
    public Empleado? EmpleadoResponsable { get; init; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string EstadoDesvio { get; set; } = string.Empty;
    public List<RegistroHora> RegistrosHoras { get; set; } = new List<RegistroHora>();
}
