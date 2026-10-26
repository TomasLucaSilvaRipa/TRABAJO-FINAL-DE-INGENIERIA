using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RespuestaSoporteInicial
{
    public RespuestaSoporteInicial(bool disponible) => Disponible = disponible;
    public bool Disponible { get; set; }
}
