using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class FiltroDesvioHoras
{
    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue ? (Proyecto?.ID == value.Value ? Proyecto : new Proyecto { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public int? IdTarea
    {
        get => Tarea?.ID;
        set => Tarea = value.HasValue ? (Tarea?.ID == value.Value ? Tarea : new Tarea { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public Empleado? Empleado { get; set; }

    public int? IdEmpleado
    {
        get => Empleado?.ID;
        set => Empleado = value.HasValue ? (Empleado?.ID == value.Value ? Empleado : new Empleado { ID = value.Value }) : null;
    }
    public string? EstadoTarea { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? NivelDesvio { get; set; }
}
