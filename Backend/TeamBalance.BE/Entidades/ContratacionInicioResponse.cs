namespace TeamBalance.BE.Entidades;

public sealed class ContratacionInicioResponse
{
    public ContratacionInicioResponse(string urlPago, string referencia)
    {
        UrlPago = urlPago;
        Referencia = referencia;
    }

    public string UrlPago { get; set; }

    public string Referencia { get; set; }
}
