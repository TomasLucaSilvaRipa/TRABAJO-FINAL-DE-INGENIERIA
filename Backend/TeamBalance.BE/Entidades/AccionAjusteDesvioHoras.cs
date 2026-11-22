using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class AccionAjusteDesvioHoras
{
    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new();

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
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
    public string Observacion { get; set; } = string.Empty;
}
