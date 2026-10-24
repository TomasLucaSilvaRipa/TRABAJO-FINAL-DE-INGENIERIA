namespace TeamBalance.BE.Entidades;

public class DetalleRiesgoProyecto
{
    public DetalleRiesgoProyecto()
    {
        Causas = new List<CausaRiesgo>();
        TareasPendientes = new List<TareaRiesgoRetraso>();
        HitosTareas = new List<HitoTareaProyecto>();
    }

    public DetalleRiesgoProyecto(int idProyecto, string nombreProyecto, string nombreCliente, string nombrePM, DateTime? fechaInicio, DateTime? deadline, DateTime? fechaEstimadaFinalizacion, int diasPosibleRetraso, decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance, string nivelRiesgo, List<CausaRiesgo> causas, List<TareaRiesgoRetraso> tareasPendientes, List<HitoTareaProyecto> hitosTareas)
    {
        IdProyecto = idProyecto;
        NombreProyecto = nombreProyecto;
        NombreCliente = nombreCliente;
        NombrePM = nombrePM;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        FechaEstimadaFinalizacion = fechaEstimadaFinalizacion;
        DiasPosibleRetraso = diasPosibleRetraso;
        HorasRestantes = horasRestantes;
        DisponibilidadEquipo = disponibilidadEquipo;
        RitmoAvance = ritmoAvance;
        NivelRiesgo = nivelRiesgo;
        Causas = causas;
        TareasPendientes = tareasPendientes;
        HitosTareas = hitosTareas;
    }

    public int IdProyecto { get; set; }

    public string NombreProyecto { get; set; } = string.Empty;

    public string NombreCliente { get; set; } = string.Empty;

    public string NombrePM { get; set; } = string.Empty;

    public DateTime? FechaInicio { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? FechaEstimadaFinalizacion { get; set; }

    public int DiasPosibleRetraso { get; set; }

    public decimal HorasRestantes { get; set; }

    public decimal DisponibilidadEquipo { get; set; }

    public decimal RitmoAvance { get; set; }

    public string NivelRiesgo { get; set; } = string.Empty;

    public List<CausaRiesgo> Causas { get; set; }

    public List<TareaRiesgoRetraso> TareasPendientes { get; set; }

    public List<HitoTareaProyecto> HitosTareas { get; set; }
}

public class HitoTareaProyecto
{
    public HitoTareaProyecto()
    {
    }

    public HitoTareaProyecto(int idTarea, string titulo, DateTime? fechaInicio, DateTime? deadline, decimal horasEstimadas, decimal porcentajeAvance, decimal horasRestantes)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        HorasEstimadas = horasEstimadas;
        PorcentajeAvance = porcentajeAvance;
        HorasRestantes = horasRestantes;
    }

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public decimal HorasRestantes { get; set; }
}

public class TareaRiesgoRetraso
{
    public TareaRiesgoRetraso()
    {
    }

    public TareaRiesgoRetraso(int idTarea, string titulo, string estado, bool bloqueada, DateTime? fechaInicio, DateTime? deadline, decimal horasEstimadas, decimal porcentajeAvance, decimal horasRestantes)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        Estado = estado;
        Bloqueada = bloqueada;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        HorasEstimadas = horasEstimadas;
        PorcentajeAvance = porcentajeAvance;
        HorasRestantes = horasRestantes;
    }

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool Bloqueada { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public decimal HorasRestantes { get; set; }
}
