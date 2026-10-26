namespace TeamBalance.BE.Entidades;
public class SeguimientoConvenienciaOperativa
{
    public int ID { get; set; }
    public int IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string Usuario { get; set; } = string.Empty;
}
