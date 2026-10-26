namespace TeamBalance.BE.Entidades;
public class DetalleConvenienciaOperativa
{
    public ConvenienciaOperativaProyecto Proyecto { get; set; } = new ConvenienciaOperativaProyecto();
    public List<ComposicionCostoOperativo> ComposicionCosto { get; set; } = new List<ComposicionCostoOperativo>();
    public List<SeguimientoConvenienciaOperativa> Seguimientos { get; set; } = new List<SeguimientoConvenienciaOperativa>();
}
