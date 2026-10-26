namespace TeamBalance.BE.Entidades;
public class ResultadoInicioSesionDosFactores
{
    public ResultadoInicioSesionDosFactores()
    {
    }

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
