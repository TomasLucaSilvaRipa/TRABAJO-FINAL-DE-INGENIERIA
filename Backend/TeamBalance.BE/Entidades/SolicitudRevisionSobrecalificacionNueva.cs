using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionSobrecalificacionNueva
{
    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new();

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new();

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value };
    }
    public string Observacion { get; set; } = string.Empty;
}
