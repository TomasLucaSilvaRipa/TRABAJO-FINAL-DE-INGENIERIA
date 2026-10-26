namespace TeamBalance.BE.Entidades;
public class DesvioHorasProyecto
{
    public int IdProyecto { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<DesvioHorasTarea> Tareas { get; set; } = new List<DesvioHorasTarea>();
}
