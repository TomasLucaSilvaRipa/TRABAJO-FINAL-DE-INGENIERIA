namespace TeamBalance.BE.Entidades;
public class EstadoTelegram
{
    public EstadoTelegram()
    {
    }

    public EstadoTelegram(bool vinculado, string estadoSolicitud, DateTime? fechaExpiracionSolicitud, DateTime? fechaVinculacion)
    {
        Vinculado = vinculado;
        EstadoSolicitud = estadoSolicitud;
        FechaExpiracionSolicitud = fechaExpiracionSolicitud;
        FechaVinculacion = fechaVinculacion;
    }

    public bool Vinculado { get; set; }
    public string EstadoSolicitud { get; set; } = string.Empty;
    public DateTime? FechaExpiracionSolicitud { get; set; }
    public DateTime? FechaVinculacion { get; set; }
}
