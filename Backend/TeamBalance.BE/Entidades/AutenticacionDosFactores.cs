namespace TeamBalance.BE.Entidades;

public class ConfiguracionAutenticacionDosFactores
{
    public int IdUsuario { get; set; }
    public string? SecretoTotpCifrado { get; set; }
    public string? SecretoPendienteCifrado { get; set; }
    public DateTime? FechaExpiracionPendiente { get; set; }
    public int IntentosPendientes { get; set; }
    public bool TotpHabilitado { get; set; }
    public DateTime? FechaActivacionTotp { get; set; }
    public long? UltimoPasoTotp { get; set; }
}

public class DesafioAutenticacionDosFactores
{
    public bool Valido { get; set; }
    public int? IdUsuario { get; set; }
    public bool MantenerSesion { get; set; }
}

public class ResultadoInicioSesionDosFactores
{
    public InicioSesionResultado? InicioSesion { get; set; }
    public bool RequiereDosFactores { get; set; }
    public string? TokenDesafio { get; set; }
    public DateTime? FechaExpiracionDesafio { get; set; }
}

public class PreparacionTotp
{
    public string UriOtpAuth { get; set; } = string.Empty;
    public string ClaveManual { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}

public class EstadoAutenticacionDosFactores
{
    public bool TotpHabilitado { get; set; }
    public DateTime? FechaActivacionTotp { get; set; }
}
