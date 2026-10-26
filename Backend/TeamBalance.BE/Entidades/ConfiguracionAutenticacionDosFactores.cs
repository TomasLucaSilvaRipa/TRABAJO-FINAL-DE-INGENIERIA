namespace TeamBalance.BE.Entidades;
public class ConfiguracionAutenticacionDosFactores
{
    public ConfiguracionAutenticacionDosFactores()
    {
    }

    public ConfiguracionAutenticacionDosFactores(int idUsuario, string? secretoTotpCifrado, string? secretoPendienteCifrado, DateTime? fechaExpiracionPendiente, int intentosPendientes, bool totpHabilitado, DateTime? fechaActivacionTotp, long? ultimoPasoTotp)
    {
        IdUsuario = idUsuario;
        SecretoTotpCifrado = secretoTotpCifrado;
        SecretoPendienteCifrado = secretoPendienteCifrado;
        FechaExpiracionPendiente = fechaExpiracionPendiente;
        IntentosPendientes = intentosPendientes;
        TotpHabilitado = totpHabilitado;
        FechaActivacionTotp = fechaActivacionTotp;
        UltimoPasoTotp = ultimoPasoTotp;
    }

    public int IdUsuario { get; set; }
    public string? SecretoTotpCifrado { get; set; }
    public string? SecretoPendienteCifrado { get; set; }
    public DateTime? FechaExpiracionPendiente { get; set; }
    public int IntentosPendientes { get; set; }
    public bool TotpHabilitado { get; set; }
    public DateTime? FechaActivacionTotp { get; set; }
    public long? UltimoPasoTotp { get; set; }
}
