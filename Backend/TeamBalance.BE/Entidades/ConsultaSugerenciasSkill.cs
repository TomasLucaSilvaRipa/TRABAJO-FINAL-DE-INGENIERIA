namespace TeamBalance.BE.Entidades;
public class ConsultaSugerenciasSkill
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Skill Skill { get; set; } = new();

    // Adaptador para el contrato plano.
    public int IdSkill
    {
        get => Skill.ID;
        set => Skill = Skill.ID == value ? Skill : new() { ID = value };
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto Proyecto { get; set; } = new();

    // Adaptador para el contrato plano.
    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }
}
