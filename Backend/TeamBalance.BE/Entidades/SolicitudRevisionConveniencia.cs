using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionConveniencia
{
    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Cliente Cliente { get; set; } = new();

    public int IdCliente
    {
        get => Cliente.ID;
        set => Cliente = Cliente.ID == value ? Cliente : new() { ID = value, Nombre = Cliente.Nombre };
    }
    [JsonIgnore, ReferenciaInterna]
    public PM PMResponsable { get; set; } = new();

    public int IdPMResponsable
    {
        get => PMResponsable.ID;
        set => PMResponsable = PMResponsable.ID == value ? PMResponsable : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Usuario UsuarioSolicitante { get; set; } = new();

    public int IdUsuarioSolicitante
    {
        get => UsuarioSolicitante.ID;
        set => UsuarioSolicitante = UsuarioSolicitante.ID == value ? UsuarioSolicitante : new() { ID = value };
    }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreCliente
    {
        get => Cliente.Nombre;
        set => Cliente.Nombre = value;
    }
    public string NombrePM { get; set; } = string.Empty;
}
