namespace TeamBalance.BE.Entidades;
public sealed class EnviarMensajeSoporteRequest
{
    public int IdConsulta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? Estado { get; set; }
}
