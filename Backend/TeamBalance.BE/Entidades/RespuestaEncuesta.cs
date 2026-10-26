namespace TeamBalance.BE.Entidades;
public sealed class RespuestaEncuesta
{
    public int IdEncuesta { get; set; }
    public string IdentificadorParticipante { get; set; } = string.Empty;
    public List<RespuestaPreguntaEncuesta> Respuestas { get; set; } = new List<RespuestaPreguntaEncuesta>();
}
