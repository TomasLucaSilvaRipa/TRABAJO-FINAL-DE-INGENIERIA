namespace TeamBalance.BE.Entidades;
public class ComunicacionDesvioHoras
{
    [System.Text.Json.Serialization.JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new();

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }
    public FiltroDesvioHoras Filtro { get; set; } = new FiltroDesvioHoras();
}
