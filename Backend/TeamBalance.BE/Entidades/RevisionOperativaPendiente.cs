using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class RevisionOperativaPendiente
{
    public RevisionOperativaPendiente()
    {
    }

    public RevisionOperativaPendiente(int id, string tipo, int? idCliente, string cliente, string referencia, string nombreProyecto, string causaPrincipal, string accionSugerida, string estado, DateTime fechaAlta)
    {
        ID = id;
        Tipo = tipo;
        IdCliente = idCliente;
        Cliente = cliente;
        Referencia = referencia;
        NombreProyecto = nombreProyecto;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Estado = estado;
        FechaAlta = fechaAlta;
    }

    public int ID { get; set; }
    public string Tipo { get; set; } = string.Empty;
    private string _nombreClienteSinReferencia = string.Empty;

    [JsonIgnore, ReferenciaInterna]
    public Cliente? ClienteRelacionado { get; set; }

    public int? IdCliente
    {
        get => ClienteRelacionado?.ID;
        set
        {
            if (value is int id)
            {
                if (ClienteRelacionado?.ID != id)
                    ClienteRelacionado = new Cliente { ID = id, Nombre = Cliente };
            }
            else
            {
                _nombreClienteSinReferencia = Cliente;
                ClienteRelacionado = null;
            }
        }
    }

    public string Cliente
    {
        get => ClienteRelacionado is Cliente cliente ? cliente.Nombre : _nombreClienteSinReferencia;
        set
        {
            if (ClienteRelacionado is Cliente cliente)
                cliente.Nombre = value;
            else
                _nombreClienteSinReferencia = value;
        }
    }
    public string Referencia { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
