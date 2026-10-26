namespace TeamBalance.BE.Entidades;
public class TareaResumenCorreo
{
    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Prioridad { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public string? TokenEnProgreso { get; set; }
    public string? TokenFinalizada { get; set; }
}
