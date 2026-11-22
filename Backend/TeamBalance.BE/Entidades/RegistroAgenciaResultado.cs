using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class RegistroAgenciaResultado
{
    public RegistroAgenciaResultado()
    {
    }

    public RegistroAgenciaResultado(int idAgencia, int idUsuario)
    {
        IdAgencia = idAgencia;
        IdUsuario = idUsuario;
    }

    [JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value };
    }
}
