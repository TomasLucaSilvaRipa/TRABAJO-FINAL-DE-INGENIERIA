namespace TeamBalance.BE.Entidades;
public class PreparacionTotp
{
    public PreparacionTotp()
    {
    }

    public PreparacionTotp(string uriOtpAuth, string claveManual, DateTime fechaExpiracion)
    {
        UriOtpAuth = uriOtpAuth;
        ClaveManual = claveManual;
        FechaExpiracion = fechaExpiracion;
    }

    public string UriOtpAuth { get; set; } = string.Empty;
    public string ClaveManual { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}
