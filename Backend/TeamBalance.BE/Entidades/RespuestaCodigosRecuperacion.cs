using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaCodigosRecuperacion
{
    public RespuestaCodigosRecuperacion(List<string> codigosRecuperacion) => CodigosRecuperacion = codigosRecuperacion;
    public List<string> CodigosRecuperacion { get; set; }
}
