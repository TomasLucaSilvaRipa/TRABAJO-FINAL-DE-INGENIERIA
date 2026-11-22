using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroRiesgoRetraso
{
    public FiltroRiesgoRetraso()
    {
    }

    public FiltroRiesgoRetraso(int? idProyecto, int? idCliente, int? idPM, string? nivelRiesgo, int? diasHastaDeadlineMaximo)
    {
        Proyecto = idProyecto is int valorProyecto ? new Proyecto { ID = valorProyecto } : null;
        Cliente = idCliente is int valorCliente ? new Cliente { ID = valorCliente } : null;
        PM = idPM is int valorPM ? new PM { ID = valorPM } : null;
        NivelRiesgo = nivelRiesgo;
        DiasHastaDeadlineMaximo = diasHastaDeadlineMaximo;
    }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public PM? PM { get; set; }

    public string? NivelRiesgo { get; set; }

    public int? DiasHastaDeadlineMaximo { get; set; }
}
