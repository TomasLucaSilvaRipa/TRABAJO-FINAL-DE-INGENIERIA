namespace TeamBalance.BE.Entidades;
public class DetalleRiesgoProyecto
{
    public DetalleRiesgoProyecto(Proyecto proyecto, DateTime? fechaEstimadaFinalizacion, int diasPosibleRetraso, decimal horasRestantes, decimal disponibilidadEquipo, decimal ritmoAvance, string nivelRiesgo, List<CausaRiesgo> causas, List<TareaRiesgoRetraso> tareasPendientes, List<HitoTareaProyecto> hitosTareas)
    {
        Proyecto = proyecto;
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

    public Proyecto Proyecto { get; }
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
