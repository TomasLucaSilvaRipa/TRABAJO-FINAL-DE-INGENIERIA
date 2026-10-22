namespace TeamBalance.BE.Entidades;

public class FiltroCoberturaSkill
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdCliente { get; set; }
}

public class TareaCoberturaSkill
{
    public TareaCoberturaSkill(int idTarea, int idSkill, string nombreSkill, string? categoriaSkill, string tituloTarea, decimal horasEstimadas, DateTime deadline, int idProyecto, string nombreProyecto, string nombreCliente)
    {
        IdTarea = idTarea;
        IdSkill = idSkill;
        NombreSkill = nombreSkill;
        CategoriaSkill = categoriaSkill;
        TituloTarea = tituloTarea;
        HorasEstimadas = horasEstimadas;
        Deadline = deadline;
        IdProyecto = idProyecto;
        NombreProyecto = nombreProyecto;
        NombreCliente = nombreCliente;
    }

    public int IdTarea { get; set; }
    public int IdSkill { get; set; }
    public string NombreSkill { get; set; } = string.Empty;
    public string? CategoriaSkill { get; set; }
    public string TituloTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public DateTime Deadline { get; set; }
    public int IdProyecto { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
}

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

public class AnalisisCoberturaSkill
{
    public int IdSkill { get; set; }
    public string NombreSkill { get; set; } = string.Empty;
    public string? CategoriaSkill { get; set; }
    public decimal DemandaHoras { get; set; }
    public decimal CapacidadDisponibleHoras { get; set; }
    public decimal CapacidadTotalHoras { get; set; }
    public decimal PorcentajeCobertura { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public int CantidadProyectosAfectados { get; set; }
    public string TipoBrecha { get; set; } = string.Empty;
    public string Criticidad { get; set; } = string.Empty;
    public int PuntajeCriticidad { get; set; }
    public DateTime? DeadlineMasProximo { get; set; }
    public string AccionSugerida { get; set; } = string.Empty;
    public List<TareaCoberturaSkill> TareasAfectadas { get; set; } = new List<TareaCoberturaSkill>();
    public List<EmpleadoCoberturaSkill> EmpleadosRelacionados { get; set; } = new List<EmpleadoCoberturaSkill>();
}

public class RecomendacionSkill
{
    public int ID { get; set; }
    public int IdSkill { get; set; }
    public string TipoAccion { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
}

public class ConsultaSugerenciasSkill
{
    public int IdSkill { get; set; }
    public int IdProyecto { get; set; }
}

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
