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
    public int IdUsuario { get; set; }
    public int? IdAgencia { get; set; }
    public int Calificacion { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreRol { get; set; } = string.Empty;
}
