using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaConsultaSoporte
{
    public RespuestaConsultaSoporte(ConsultaSoporteResumen consulta, List<MensajeSoporteDetalle> mensajes)
    {
        Consulta = consulta;
        Mensajes = mensajes;
    }

    public ConsultaSoporteResumen Consulta { get; set; }
    public List<MensajeSoporteDetalle> Mensajes { get; set; }
}
