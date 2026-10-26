namespace TeamBalance.BE.Entidades;
public class SeguimientoRiesgoRetraso
{
    public int ID { get; set; }
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public bool ComunicarResponsable { get; set; }
    public DateTime FechaAlta { get; set; }
    public string Usuario { get; set; } = string.Empty;
}
