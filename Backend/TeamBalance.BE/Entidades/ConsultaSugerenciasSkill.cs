namespace TeamBalance.BE.Entidades;
public class ConsultaSugerenciasSkill
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Skill Skill { get; set; } = new();

    public int IdSkill
    {
        get => Skill.ID;
        set => Skill = Skill.ID == value ? Skill : new() { ID = value };
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto Proyecto { get; set; } = new();

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }
}
