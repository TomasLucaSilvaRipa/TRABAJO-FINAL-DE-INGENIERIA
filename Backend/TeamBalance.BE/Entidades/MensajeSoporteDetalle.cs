namespace TeamBalance.BE.Entidades;
public sealed class MensajeSoporteDetalle
{
    public MensajeSoporteDetalle(int id, int idUsuario, string nombreUsuario, bool esSoporte, string mensaje, DateTime fecha)
    {
        ID = id;
        IdUsuario = idUsuario;
        NombreUsuario = nombreUsuario;
        EsSoporte = esSoporte;
        Mensaje = mensaje;
        Fecha = fecha;
    }

    public int ID { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public bool EsSoporte { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
