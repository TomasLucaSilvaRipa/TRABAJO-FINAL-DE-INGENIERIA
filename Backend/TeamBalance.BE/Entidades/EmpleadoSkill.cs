namespace TeamBalance.BE.Entidades;

public class EmpleadoSkill
{
    public EmpleadoSkill() { }
    public EmpleadoSkill(int id, int idEmpleado, int idSkill, string? nivel, bool activo) { ID = id; IdEmpleado = idEmpleado; IdSkill = idSkill; Nivel = nivel; Activo = activo; }
    public int ID { get; set; }
    public int IdEmpleado { get; set; }
    public int IdSkill { get; set; }
    public string? Nivel { get; set; }
    public bool Activo { get; set; }
}
