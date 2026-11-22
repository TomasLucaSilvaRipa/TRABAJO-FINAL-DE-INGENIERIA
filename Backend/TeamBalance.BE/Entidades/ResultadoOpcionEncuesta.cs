using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class ResultadoOpcionEncuesta
{
    [JsonIgnore, ReferenciaInterna]
    public OpcionEncuesta Opcion { get; set; } = new();

    public int IdOpcionEncuesta
    {
        get => Opcion.ID;
        set => Opcion = Opcion.ID == value ? Opcion : new() { ID = value, TextoEs = Opcion.TextoEs, TextoEn = Opcion.TextoEn, Orden = Opcion.Orden };
    }
    public string TextoEs
    {
        get => Opcion.TextoEs;
        set => Opcion.TextoEs = value;
    }
    public string TextoEn
    {
        get => Opcion.TextoEn;
        set => Opcion.TextoEn = value;
    }
    public int Orden
    {
        get => Opcion.Orden;
        set => Opcion.Orden = value;
    }
    public int CantidadRespuestas { get; set; }
    public decimal Porcentaje { get; set; }
}
