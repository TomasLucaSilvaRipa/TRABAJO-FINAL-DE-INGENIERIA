using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class FiltroOcupacionOperativa
{
    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    public int? IdCliente
    {
        get => Cliente?.ID;
        set => Cliente = value.HasValue ? (Cliente?.ID == value.Value ? Cliente : new Cliente { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue ? (Proyecto?.ID == value.Value ? Proyecto : new Proyecto { ID = value.Value }) : null;
    }
    [JsonIgnore, ReferenciaInterna]
    public Empleado? Empleado { get; set; }

    public int? IdEmpleado
    {
        get => Empleado?.ID;
        set => Empleado = value.HasValue ? (Empleado?.ID == value.Value ? Empleado : new Empleado { ID = value.Value }) : null;
    }
    public string? EstadoCarga { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
