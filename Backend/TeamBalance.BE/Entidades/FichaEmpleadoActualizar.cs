using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public sealed class FichaEmpleadoActualizar
{
    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int ID { get => Empleado.ID; set => Empleado.ID = value; }
    public List<EmpleadoSkill> EmpleadoSkills
    {
        get => Empleado.EmpleadoSkills;
        set => Empleado.EmpleadoSkills = value;
    }
    public DisponibilidadBase? DisponibilidadBase
    {
        get => Empleado.DisponibilidadBase;
        set => Empleado.DisponibilidadBase = value;
    }
}
