using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class NuevoSeguimientoConvenienciaOperativa
{
    [JsonIgnore, ReferenciaInterna]
    public Cliente Cliente { get; set; } = new();

    public int IdCliente
    {
        get => Cliente.ID;
        set => Cliente = Cliente.ID == value ? Cliente : new() { ID = value };
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
    public string Observacion { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
