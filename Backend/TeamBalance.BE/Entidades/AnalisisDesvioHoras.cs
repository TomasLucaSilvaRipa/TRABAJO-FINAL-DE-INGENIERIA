namespace TeamBalance.BE.Entidades;
public class AnalisisDesvioHoras
{
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public List<DesvioHorasProyecto> Proyectos { get; set; } = new List<DesvioHorasProyecto>();
}
