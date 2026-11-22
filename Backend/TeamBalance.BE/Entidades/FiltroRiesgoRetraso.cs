using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class FiltroRiesgoRetraso
{
    public FiltroRiesgoRetraso()
    {
    }

    public FiltroRiesgoRetraso(int? idProyecto, int? idCliente, int? idPM, string? nivelRiesgo, int? diasHastaDeadlineMaximo)
    {
        IdProyecto = idProyecto;
        IdCliente = idCliente;
        IdPM = idPM;
        NivelRiesgo = nivelRiesgo;
        DiasHastaDeadlineMaximo = diasHastaDeadlineMaximo;
    }

    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue
            ? (Proyecto?.ID == value.Value ? Proyecto : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public Cliente? Cliente { get; set; }

    public int? IdCliente
    {
        get => Cliente?.ID;
        set => Cliente = value.HasValue
            ? (Cliente?.ID == value.Value ? Cliente : new() { ID = value.Value })
            : null;
    }

    [JsonIgnore, ReferenciaInterna]
    public PM? PM { get; set; }

    public int? IdPM
    {
        get => PM?.ID;
        set => PM = value.HasValue
            ? (PM?.ID == value.Value ? PM : new() { ID = value.Value })
            : null;
    }

    public string? NivelRiesgo { get; set; }

    public int? DiasHastaDeadlineMaximo { get; set; }
}
