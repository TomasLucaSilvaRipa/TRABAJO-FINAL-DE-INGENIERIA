namespace TeamBalance.BE.Entidades;
public class DetalleDesvioHorasTarea
{
    public int IdProyecto { get; set; }
    public int IdTarea { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string EstadoDesvio { get; set; } = string.Empty;
    public List<RegistroHora> RegistrosHoras { get; set; } = new List<RegistroHora>();
}
