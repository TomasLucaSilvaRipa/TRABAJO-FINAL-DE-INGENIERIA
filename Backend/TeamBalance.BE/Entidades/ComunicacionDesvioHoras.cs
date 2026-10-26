namespace TeamBalance.BE.Entidades;
public class ComunicacionDesvioHoras
{
    public int IdProyecto { get; set; }
    public FiltroDesvioHoras Filtro { get; set; } = new FiltroDesvioHoras();
}
