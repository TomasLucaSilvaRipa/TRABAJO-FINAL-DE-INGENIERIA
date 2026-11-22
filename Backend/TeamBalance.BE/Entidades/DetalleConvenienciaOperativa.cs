namespace TeamBalance.BE.Entidades;
public class DetalleConvenienciaOperativa
{
    public required ConvenienciaOperativaProyecto Proyecto { get; init; }
    public List<ComposicionCostoOperativo> ComposicionCosto { get; set; } = new List<ComposicionCostoOperativo>();
    public List<SeguimientoConvenienciaOperativa> Seguimientos { get; set; } = new List<SeguimientoConvenienciaOperativa>();
}
