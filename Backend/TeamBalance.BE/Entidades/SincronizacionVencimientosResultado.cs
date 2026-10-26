namespace TeamBalance.BE.Entidades;
public sealed class SincronizacionVencimientosResultado
{
    public SincronizacionVencimientosResultado(int idSuscripcion, int idAgencia, string estado)
    {
        IdSuscripcion = idSuscripcion;
        IdAgencia = idAgencia;
        Estado = estado;
    }

    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public string Estado { get; set; } = string.Empty;
}
