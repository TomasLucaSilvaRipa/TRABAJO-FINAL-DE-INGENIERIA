using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class OcupacionCliente
{
    [JsonIgnore, ReferenciaInterna]
    public Cliente Cliente { get; set; } = new() { Nombre = string.Empty };

    public int IdCliente
    {
        get => Cliente.ID;
        set => Cliente = Cliente.ID == value ? Cliente : new() { ID = value, Nombre = Cliente.Nombre };
    }
    public string NombreCliente
    {
        get => Cliente.Nombre;
        set => Cliente.Nombre = value;
    }
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionProyecto> Proyectos { get; set; } = new List<OcupacionProyecto>();
}
