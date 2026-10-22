namespace TeamBalance.BE.Entidades;

public class ResultadoRegistroAgenciaValidacion
{
    public ResultadoRegistroAgenciaValidacion() { }
    public ResultadoRegistroAgenciaValidacion(string tokenGestion, DateTime fechaExpiracion)
    {
        TokenGestion = tokenGestion;
        FechaExpiracion = fechaExpiracion;
    }

    public string TokenGestion { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}
