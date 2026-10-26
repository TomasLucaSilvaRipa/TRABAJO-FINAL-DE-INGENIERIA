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

    public int IdEmpleado { get; set; }
    public int IdSkill { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public string NivelSkill { get; set; } = string.Empty;
    public decimal HorasSemanales { get; set; }
    public decimal CargaActual { get; set; }
    public decimal HorasDisponibles { get; set; }
}
