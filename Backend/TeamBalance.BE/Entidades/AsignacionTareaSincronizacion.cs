using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class AsignacionTareaSincronizacion
{
    public AsignacionTareaSincronizacion(AsignacionTarea asignacion)
    {
        Empleado = asignacion.Empleado;
        HorasAsignadas = asignacion.HorasAsignadas;
        Motivo = string.IsNullOrWhiteSpace(asignacion.Motivo) ? null : asignacion.Motivo.Trim();
    }

    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; }

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new Empleado { ID = value };
    }
    public decimal HorasAsignadas { get; set; }
    public string? Motivo { get; set; }
}
