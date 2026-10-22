namespace Teambalance.API.Models;

public class CodigoAutenticacionDosFactoresRequest
{
    public string? Codigo { get; set; }
}

public class ConfirmarInicioSesionDosFactoresRequest
{
    public string? TokenDesafio { get; set; }
    public string? Codigo { get; set; }
}

public class DesafioInicioSesionDosFactoresResponse
{
    public DesafioInicioSesionDosFactoresResponse(string tokenDesafio, DateTime fechaExpiracion)
    {
        TokenDesafio = tokenDesafio;
        FechaExpiracion = fechaExpiracion;
    }

    public bool RequiereDosFactores { get; } = true;
    public string TokenDesafio { get; }
    public DateTime FechaExpiracion { get; }
}
