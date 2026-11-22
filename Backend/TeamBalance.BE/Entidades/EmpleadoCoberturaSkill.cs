using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class EmpleadoCoberturaSkill
{
    public EmpleadoCoberturaSkill(int idEmpleado, int idSkill, string nombreEmpleado, string seniority, string nivelSkill, decimal horasSemanales, decimal cargaActual)
    {
        IdEmpleado = idEmpleado;
        IdSkill = idSkill;
        NombreEmpleado = nombreEmpleado;
        Seniority = seniority;
        NivelSkill = nivelSkill;
        HorasSemanales = horasSemanales;
        CargaActual = cargaActual;
    }

    [JsonIgnore]
    public EmpleadoSkill Habilidad { get; set; } = new()
    {
        Empleado = new Empleado { Seniority = string.Empty },
        Nivel = string.Empty
    };

    public int IdEmpleado { get => Habilidad.Empleado.ID; set => Habilidad.Empleado.ID = value; }
    public int IdSkill { get => Habilidad.Skill.ID; set => Habilidad.Skill.ID = value; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Seniority { get => Habilidad.Empleado.Seniority; set => Habilidad.Empleado.Seniority = value; }
    public string NivelSkill { get => Habilidad.Nivel!; set => Habilidad.Nivel = value; }
    public decimal HorasSemanales { get; set; }
    public decimal CargaActual { get; set; }
    public decimal HorasDisponibles { get; set; }
}
