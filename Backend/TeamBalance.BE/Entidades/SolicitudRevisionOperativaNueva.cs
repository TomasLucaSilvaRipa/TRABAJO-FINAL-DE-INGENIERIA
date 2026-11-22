using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionOperativaNueva
{
    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value };
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
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public int? IdTarea
    {
        get => Tarea?.ID;
        set => Tarea = value.HasValue
            ? (Tarea?.ID == value.Value ? Tarea : new() { ID = value.Value })
            : null;
    }
}
