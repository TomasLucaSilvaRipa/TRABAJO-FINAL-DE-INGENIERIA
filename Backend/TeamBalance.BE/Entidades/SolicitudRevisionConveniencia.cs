namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionConveniencia
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdCliente { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string NombrePM { get; set; } = string.Empty;
}
