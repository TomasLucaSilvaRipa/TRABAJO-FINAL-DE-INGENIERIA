using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

/// <summary>
/// Datos editables de la ficha laboral. No reutiliza <see cref="Empleado"/>
/// porque ese tipo también contiene los datos obligatorios de la cuenta de usuario.
/// </summary>
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
