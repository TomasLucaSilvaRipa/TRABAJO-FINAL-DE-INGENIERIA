namespace TeamBalance.BE.Entidades;

public class CredencialCifrada
{
    public string DatosCifrados { get; set; } = string.Empty;
    public string ClaveCifrada { get; set; } = string.Empty;
    public string VectorInicializacion { get; set; } = string.Empty;
    public string TokenRecaptcha { get; set; } = string.Empty;
}
