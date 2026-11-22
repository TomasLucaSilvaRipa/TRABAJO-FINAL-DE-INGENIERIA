using System;
namespace TeamBalance.BE.Entidades;
using System.Text.Json.Serialization;

public class FiltroBitacora
{
    public FiltroBitacora() { }

    public FiltroBitacora(int? idAgencia = null, DateTime? desde = null, DateTime? hasta = null, int? idUsuario = null, string? entidad = null, string? accion = null, ResultadoBitacora? resultado = null, CriticidadBitacora? criticidad = null, ModuloBitacora? modulo = null)
    {
        IdAgencia = idAgencia;
        Desde = desde;
        Hasta = hasta;
        IdUsuario = idUsuario;
        Entidad = entidad;
        Accion = accion;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
    }

    [JsonIgnore, ReferenciaInterna]
    public Agencia? Agencia { get; set; }

    public int? IdAgencia
    {
        get => Agencia?.ID;
        set => Agencia = value.HasValue ? (Agencia?.ID == value.Value ? Agencia : new Agencia { ID = value.Value }) : null;
    }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario? Usuario { get; set; }

    public int? IdUsuario
    {
        get => Usuario?.ID;
        set => Usuario = value.HasValue ? (Usuario?.ID == value.Value ? Usuario : new Usuario { ID = value.Value }) : null;
    }
    public string? Entidad { get; set; }
    public string? Accion { get; set; }
    public ResultadoBitacora? Resultado { get; set; }
    public CriticidadBitacora? Criticidad { get; set; }
    public ModuloBitacora? Modulo { get; set; }
}
