namespace TeamBalance.BE.Entidades;
public sealed class ResultadoGestionSuscripcion
{
    public ResultadoGestionSuscripcion(string mensaje, string estado, Suscripcion? suscripcion = null)
    {
        Mensaje = mensaje;
        Estado = estado;
        Suscripcion = suscripcion;
    }

    public string Mensaje { get; set; }
    public string Estado { get; set; }
    public Suscripcion? Suscripcion { get; set; }
}
