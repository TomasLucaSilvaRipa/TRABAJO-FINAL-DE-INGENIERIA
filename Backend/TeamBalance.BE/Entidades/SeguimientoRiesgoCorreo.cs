using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class SeguimientoRiesgoCorreo
{
    public DestinatarioDesvioHoras Destinatario { get; set; } = new();
    public Proyecto Proyecto { get; set; } = new();
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public string? TituloTarea
    {
        get => Tarea?.Titulo;
        set => Tarea = value is null ? null : new() { Titulo = value };
    }
    public string Observacion { get; set; } = string.Empty;
}
