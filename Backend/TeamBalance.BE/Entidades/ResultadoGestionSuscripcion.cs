namespace TeamBalance.BE.Entidades;
public sealed class ResultadoGestionSuscripcion
{
    public ResultadoGestionSuscripcion(string mensaje, string estado, Suscripcion? suscripcion = null)
    {
        Mensaje = mensaje;
        Estado = estado;
        Suscripcion = suscripcion;
    }

    public string? UrlPagoSiguiente { get; set; }
    public string? ReferenciaOperacion { get; set; }
    public bool PagoConfirmado { get; set; }
    public int PagosCompletados { get; set; }
    public int PagosTotales { get; set; }
    public string Mensaje { get; set; }
    public string Estado { get; set; }
    public Suscripcion? Suscripcion { get; set; }
}
