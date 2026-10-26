namespace TeamBalance.BE.Entidades;
public class DesvioHorasTarea
{
    public int IdTarea { get; set; }
    public int? IdEmpleadoResponsable { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
}
