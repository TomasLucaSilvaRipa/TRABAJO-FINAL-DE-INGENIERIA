namespace TeamBalance.BE.Entidades;

public class DetalleRiesgoProyecto
{
    public DetalleRiesgoProyecto()
    {
        Causas = new List<CausaRiesgo>();
    }

    public DetalleRiesgoProyecto(int idProyecto, string nombreProyecto, string nombreCliente, string nombrePM, DateTime? deadline, DateTime? fechaEstimadaFinalizacion, int diasPosibleRetraso, decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance, string nivelRiesgo, List<CausaRiesgo> causas)
    {
        IdProyecto = idProyecto;
        NombreProyecto = nombreProyecto;
        NombreCliente = nombreCliente;
        NombrePM = nombrePM;
        Deadline = deadline;
        FechaEstimadaFinalizacion = fechaEstimadaFinalizacion;
        DiasPosibleRetraso = diasPosibleRetraso;
        HorasRestantes = horasRestantes;
        DisponibilidadEquipo = disponibilidadEquipo;
        RitmoAvance = ritmoAvance;
        NivelRiesgo = nivelRiesgo;
        Causas = causas;
    }

    public int IdProyecto { get; set; }

    public string NombreProyecto { get; set; } = string.Empty;

    public string NombreCliente { get; set; } = string.Empty;

    public string NombrePM { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    public DateTime? FechaEstimadaFinalizacion { get; set; }

    public int DiasPosibleRetraso { get; set; }

    public decimal HorasRestantes { get; set; }

    public decimal DisponibilidadEquipo { get; set; }

    public decimal RitmoAvance { get; set; }

    public string NivelRiesgo { get; set; } = string.Empty;

    public List<CausaRiesgo> Causas { get; set; }
}
