namespace TeamBalance.BE.Entidades;
public class NuevoSeguimientoRiesgoRetraso
{
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public bool ComunicarResponsable { get; set; }
}
