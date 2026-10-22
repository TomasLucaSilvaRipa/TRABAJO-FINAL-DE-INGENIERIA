namespace TeamBalance.BE.Entidades;

public class ConfiguracionAutenticacionDosFactores
{
    public ConfiguracionAutenticacionDosFactores() { }

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

public class DesafioAutenticacionDosFactores
{
    public DesafioAutenticacionDosFactores() { }
    public DesafioAutenticacionDosFactores(bool valido, int? idUsuario, bool mantenerSesion)
    {
        Valido = valido;
        IdUsuario = idUsuario;
        MantenerSesion = mantenerSesion;
    }

    public bool Valido { get; set; }
    public int? IdUsuario { get; set; }
    public bool MantenerSesion { get; set; }
}

public class ResultadoInicioSesionDosFactores
{
    public ResultadoInicioSesionDosFactores() { }
    public ResultadoInicioSesionDosFactores(InicioSesionResultado? inicioSesion, bool requiereDosFactores, string? tokenDesafio, DateTime? fechaExpiracionDesafio)
    {
        InicioSesion = inicioSesion;
        RequiereDosFactores = requiereDosFactores;
        TokenDesafio = tokenDesafio;
        FechaExpiracionDesafio = fechaExpiracionDesafio;
    }

    public InicioSesionResultado? InicioSesion { get; set; }
    public bool RequiereDosFactores { get; set; }
    public string? TokenDesafio { get; set; }
    public DateTime? FechaExpiracionDesafio { get; set; }
}

public class PreparacionTotp
{
    public PreparacionTotp() { }
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

public class EstadoAutenticacionDosFactores
{
    public EstadoAutenticacionDosFactores() { }
    public EstadoAutenticacionDosFactores(bool totpHabilitado, DateTime? fechaActivacionTotp)
    {
        TotpHabilitado = totpHabilitado;
        FechaActivacionTotp = fechaActivacionTotp;
    }

    public bool TotpHabilitado { get; set; }
    public DateTime? FechaActivacionTotp { get; set; }
}
