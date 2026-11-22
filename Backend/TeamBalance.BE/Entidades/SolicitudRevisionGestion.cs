using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionGestion
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
    public Proyecto Proyecto { get; set; } = new() { Nombre = string.Empty };

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value, Nombre = Proyecto.Nombre };
    }
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public int? IdTarea
    {
        get => Tarea?.ID;
        set
        {
            if (value is int id)
            {
                if (Tarea?.ID != id)
                    Tarea = new() { ID = id, Titulo = TituloTarea! };
            }
            else
            {
                _tituloTareaSinReferencia = TituloTarea;
                Tarea = null;
            }
        }
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
    public string Tipo { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreProyecto
    {
        get => Proyecto.Nombre;
        set => Proyecto.Nombre = value;
    }
    private string? _tituloTareaSinReferencia;

    public string? TituloTarea
    {
        get => Tarea is null ? _tituloTareaSinReferencia : Tarea.Titulo;
        set
        {
            if (Tarea is null)
                _tituloTareaSinReferencia = value;
            else
                Tarea.Titulo = value!;
        }
    }
}
