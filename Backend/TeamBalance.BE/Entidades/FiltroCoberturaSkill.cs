namespace TeamBalance.BE.Entidades;
public class FiltroCoberturaSkill
{
    public Skill Skill { get; set; } = new();
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdCliente { get; set; }
}
