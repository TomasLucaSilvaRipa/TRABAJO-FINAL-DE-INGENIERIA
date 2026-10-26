namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionSobrecalificacionNueva
{
    public int IdEmpleado { get; set; }
    public int IdProyecto { get; set; }
    public int IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
