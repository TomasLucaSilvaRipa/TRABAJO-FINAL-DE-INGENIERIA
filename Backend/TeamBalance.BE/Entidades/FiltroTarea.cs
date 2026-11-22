using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroTarea
{
    public FiltroTarea()
    {
    }

    public FiltroTarea(int? idProyecto, string? estado, string? prioridad, int? idEmpleadoAsignado, int? idSkillRequerido, DateTime? deadlineDesde, DateTime? deadlineHasta)
    {
        IdProyecto = idProyecto;
        Estado = estado;
        Prioridad = prioridad;
        IdEmpleadoAsignado = idEmpleadoAsignado;
        IdSkillRequerido = idSkillRequerido;
        DeadlineDesde = deadlineDesde;
        DeadlineHasta = deadlineHasta;
    }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue
            ? (Proyecto?.ID == value.Value ? Proyecto : new() { ID = value.Value })
            : null;
    }

    public string? Estado { get; set; }

    public string? Prioridad { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Empleado? EmpleadoAsignado { get; set; }

    public int? IdEmpleadoAsignado
    {
        get => EmpleadoAsignado?.ID;
        set => EmpleadoAsignado = value.HasValue
            ? (EmpleadoAsignado?.ID == value.Value ? EmpleadoAsignado : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public Skill? SkillRequerido { get; set; }

    public int? IdSkillRequerido
    {
        get => SkillRequerido?.ID;
        set => SkillRequerido = value.HasValue
            ? (SkillRequerido?.ID == value.Value ? SkillRequerido : new() { ID = value.Value })
            : null;
    }

    public DateTime? DeadlineDesde { get; set; }

    public DateTime? DeadlineHasta { get; set; }
}
