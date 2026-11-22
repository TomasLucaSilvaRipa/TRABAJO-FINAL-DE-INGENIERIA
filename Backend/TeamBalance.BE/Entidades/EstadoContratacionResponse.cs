namespace TeamBalance.BE.Entidades;

public sealed class EstadoContratacionResponse
{
    public EstadoContratacionResponse(string referencia, string estado, bool puedeRegistrar)
    {
        Referencia = referencia;
        Estado = estado;
        PuedeRegistrar = puedeRegistrar;
    }

    public string Referencia { get; set; }

    public string Estado { get; set; }

    public bool PuedeRegistrar { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PagosCompletados { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PagosTotales { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? UrlPagoSiguiente { get; set; }
}
