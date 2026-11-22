namespace TeamBalance.BE.Entidades;
public class DesvioHorasProyecto
{
    public required Proyecto Proyecto { get; init; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<DesvioHorasTarea> Tareas { get; set; } = new List<DesvioHorasTarea>();
}
