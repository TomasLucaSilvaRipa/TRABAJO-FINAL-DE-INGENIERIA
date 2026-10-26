namespace TeamBalance.BE.Entidades;
public class PreparacionTelegram
{
    public PreparacionTelegram()
    {
    }

    public PreparacionTelegram(string enlaceProfundo, string comandoInicio, DateTime fechaExpiracion)
    {
        EnlaceProfundo = enlaceProfundo;
        ComandoInicio = comandoInicio;
        FechaExpiracion = fechaExpiracion;
    }

    public string EnlaceProfundo { get; set; } = string.Empty;
    public string ComandoInicio { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}
