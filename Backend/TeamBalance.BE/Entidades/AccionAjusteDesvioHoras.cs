namespace TeamBalance.BE.Entidades;
public class AccionAjusteDesvioHoras
{
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
