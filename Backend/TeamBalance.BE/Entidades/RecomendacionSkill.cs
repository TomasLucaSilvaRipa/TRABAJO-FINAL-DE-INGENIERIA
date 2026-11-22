using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RecomendacionSkill
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Skill Skill { get; set; } = new();

    public int IdSkill
    {
        get => Skill.ID;
        set => Skill = Skill.ID == value ? Skill : new() { ID = value };
    }
    public string TipoAccion { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
}
