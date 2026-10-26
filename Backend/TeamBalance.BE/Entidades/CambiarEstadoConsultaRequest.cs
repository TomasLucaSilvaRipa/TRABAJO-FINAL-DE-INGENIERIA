namespace TeamBalance.BE.Entidades;
public sealed class CambiarEstadoConsultaRequest
{
    public int IdConsulta { get; set; }
    public string Estado { get; set; } = string.Empty;
}
