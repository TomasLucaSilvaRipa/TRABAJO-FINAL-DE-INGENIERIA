using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudRevisionOperativa
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
    public Empleado Empleado { get; set; } = new();

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value };
    }
    [JsonIgnore, ReferenciaInterna]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set
        {
            if (value is int id)
            {
                if (Proyecto?.ID != id)
                    Proyecto = new() { ID = id, Nombre = NombreProyecto! };
            }
            else
            {
                _nombreProyectoSinReferencia = NombreProyecto;
                Proyecto = null;
            }
        }
    }
    [JsonIgnore, ReferenciaInterna]
    public Tarea? Tarea { get; set; }

    public int? IdTarea
    {
        get => Tarea?.ID;
        set => Tarea = value.HasValue
            ? (Tarea?.ID == value.Value ? Tarea : new() { ID = value.Value })
            : null;
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
    public string NombreEmpleado { get; set; } = string.Empty;
    private string _nombreProyectoSinReferencia = string.Empty;

    public string NombreProyecto
    {
        get => Proyecto is null ? _nombreProyectoSinReferencia : Proyecto.Nombre;
        set
        {
            if (Proyecto is null)
                _nombreProyectoSinReferencia = value;
            else
                Proyecto.Nombre = value;
        }
    }
    public string NombrePM { get; set; } = string.Empty;
}
