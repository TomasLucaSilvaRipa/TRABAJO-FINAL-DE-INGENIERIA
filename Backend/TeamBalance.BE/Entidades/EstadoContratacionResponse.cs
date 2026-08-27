namespace TeamBalance.BE.Entidades;

public sealed class EstadoContratacionResponse
{
    public string Referencia { get; init; } = string.Empty;

    public string Estado { get; init; } = string.Empty;

    public bool PuedeRegistrar { get; init; }
}
