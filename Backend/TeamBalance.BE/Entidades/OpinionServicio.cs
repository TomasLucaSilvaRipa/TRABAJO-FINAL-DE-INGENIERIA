using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;

public class OpinionServicio
{
    public OpinionServicio() { }

    public OpinionServicio(int id, int idUsuario, int? idAgencia, int calificacion, string titulo, string comentario, DateTime fechaAlta, string nombreUsuario, string nombreRol)
    {
        ID = id;
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Calificacion = calificacion;
        Titulo = titulo;
        Comentario = comentario;
        FechaAlta = fechaAlta;
        NombreUsuario = nombreUsuario;
        NombreRol = nombreRol;
    }

    public int ID { get; set; }
    [JsonIgnore, ReferenciaInterna]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Agencia? Agencia { get; set; }

    public int? IdAgencia
    {
        get => Agencia?.ID;
        set => Agencia = value.HasValue
            ? (Agencia?.ID == value.Value ? Agencia : new() { ID = value.Value })
            : null;
    }
    public int Calificacion { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreRol { get; set; } = string.Empty;
}
