using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroTarea
{
    public FiltroTarea()
    {
    }

    public FiltroTarea(int? idProyecto, string? estado, string? prioridad, int? idEmpleadoAsignado, int? idSkillRequerido, DateTime? deadlineDesde, DateTime? deadlineHasta)
    {
        Proyecto = idProyecto is int valorProyecto ? new Proyecto { ID = valorProyecto } : null;
        Estado = estado;
        Prioridad = prioridad;
        EmpleadoAsignado = idEmpleadoAsignado is int valorEmpleadoAsignado ? new Empleado { ID = valorEmpleadoAsignado } : null;
        SkillRequerido = idSkillRequerido is int valorSkillRequerido ? new Skill { ID = valorSkillRequerido } : null;
        DeadlineDesde = deadlineDesde;
        DeadlineHasta = deadlineHasta;
    }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public string? Estado { get; set; }

    public string? Prioridad { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Empleado? EmpleadoAsignado { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Skill? SkillRequerido { get; set; }

    public DateTime? DeadlineDesde { get; set; }

    public DateTime? DeadlineHasta { get; set; }
}
