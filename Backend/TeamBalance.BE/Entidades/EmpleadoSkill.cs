using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class EmpleadoSkill
{
    public EmpleadoSkill() { }
    public EmpleadoSkill(int id, int idEmpleado, int idSkill, string? nivel, bool activo) { ID = id; IdEmpleado = idEmpleado; IdSkill = idSkill; Nivel = nivel; Activo = activo; }
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new Empleado { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Skill Skill { get; set; } = new();

    public int IdSkill
    {
        get => Skill.ID;
        set => Skill = Skill.ID == value ? Skill : new Skill { ID = value };
    }
    public string? Nivel { get; set; }
    public bool Activo { get; set; }
}
