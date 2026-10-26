using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaVigencia
{
    public RespuestaVigencia(bool vigente) => Vigente = vigente;
    public bool Vigente { get; set; }
}
