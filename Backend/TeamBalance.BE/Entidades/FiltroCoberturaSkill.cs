using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class FiltroCoberturaSkill
{
    public Skill Skill { get; set; } = new();
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }
}
