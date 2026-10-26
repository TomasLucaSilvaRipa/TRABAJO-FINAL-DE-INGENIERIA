namespace TeamBalance.BE.Entidades;
public class NuevoSeguimientoConvenienciaOperativa
{
    public int IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}
