namespace TeamBalance.BE.Entidades;
public class AnalisisCoberturaSkill
{
    public int IdSkill { get; set; }
    public string NombreSkill { get; set; } = string.Empty;
    public string? CategoriaSkill { get; set; }
    public decimal DemandaHoras { get; set; }
    public decimal CapacidadDisponibleHoras { get; set; }
    public decimal CapacidadTotalHoras { get; set; }
    public decimal PorcentajeCobertura { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public int CantidadProyectosAfectados { get; set; }
    public string TipoBrecha { get; set; } = string.Empty;
    public string Criticidad { get; set; } = string.Empty;
    public int PuntajeCriticidad { get; set; }
    public DateTime? DeadlineMasProximo { get; set; }
    public string AccionSugerida { get; set; } = string.Empty;
    public List<TareaCoberturaSkill> TareasAfectadas { get; set; } = new List<TareaCoberturaSkill>();
    public List<EmpleadoCoberturaSkill> EmpleadosRelacionados { get; set; } = new List<EmpleadoCoberturaSkill>();
}
