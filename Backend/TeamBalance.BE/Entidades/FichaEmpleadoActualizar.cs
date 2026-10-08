namespace TeamBalance.BE.Entidades;

/// <summary>
/// Datos editables de la ficha laboral. No reutiliza <see cref="Empleado"/>
/// porque ese tipo también contiene los datos obligatorios de la cuenta de usuario.
/// </summary>
public sealed class FichaEmpleadoActualizar
{
    public int ID { get; set; }
    public List<EmpleadoSkill> EmpleadoSkills { get; set; } = new List<EmpleadoSkill>();
    public DisponibilidadBase? DisponibilidadBase { get; set; }
}
