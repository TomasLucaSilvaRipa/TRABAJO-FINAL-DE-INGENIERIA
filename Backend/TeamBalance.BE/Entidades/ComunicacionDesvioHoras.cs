namespace TeamBalance.BE.Entidades;
public class ComunicacionDesvioHoras
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto Proyecto { get; set; } = new();

    // Adaptador para el contrato plano.
    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value };
    }
    public FiltroDesvioHoras Filtro { get; set; } = new FiltroDesvioHoras();
}
