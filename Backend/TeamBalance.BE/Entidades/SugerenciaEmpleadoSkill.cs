namespace TeamBalance.BE.Entidades;
public class SugerenciaEmpleadoSkill
{
    public SugerenciaEmpleadoSkill(int idEmpleado, string nombreEmpleado, string seniority, decimal horasSemanales, decimal cargaActual, decimal horasDisponibles, bool esDelProyecto, bool tieneSkillObjetivo, int cantidadSkillsMismaArea, string skillsRelacionadas)
    {
        IdEmpleado = idEmpleado;
        NombreEmpleado = nombreEmpleado;
        Seniority = seniority;
        HorasSemanales = horasSemanales;
        CargaActual = cargaActual;
        HorasDisponibles = horasDisponibles;
        EsDelProyecto = esDelProyecto;
        TieneSkillObjetivo = tieneSkillObjetivo;
        CantidadSkillsMismaArea = cantidadSkillsMismaArea;
        SkillsRelacionadas = skillsRelacionadas;
    }

    public int IdEmpleado { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public decimal HorasSemanales { get; set; }
    public decimal CargaActual { get; set; }
    public decimal HorasDisponibles { get; set; }
    public bool EsDelProyecto { get; set; }
    public bool TieneSkillObjetivo { get; set; }
    public int CantidadSkillsMismaArea { get; set; }
    public string SkillsRelacionadas { get; set; } = string.Empty;
    public string TipoSugerencia { get; set; } = string.Empty;
    public decimal PuntajeAfinidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
