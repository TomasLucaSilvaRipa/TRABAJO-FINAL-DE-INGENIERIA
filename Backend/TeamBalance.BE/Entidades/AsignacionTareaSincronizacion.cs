using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class AsignacionTareaSincronizacion
{
    public AsignacionTareaSincronizacion(AsignacionTarea asignacion)
    {
        IdEmpleado = asignacion.IdEmpleado;
        HorasAsignadas = asignacion.HorasAsignadas;
        Motivo = string.IsNullOrWhiteSpace(asignacion.Motivo) ? null : asignacion.Motivo.Trim();
    }

    public int IdEmpleado { get; set; }
    public decimal HorasAsignadas { get; set; }
    public string? Motivo { get; set; }
}
