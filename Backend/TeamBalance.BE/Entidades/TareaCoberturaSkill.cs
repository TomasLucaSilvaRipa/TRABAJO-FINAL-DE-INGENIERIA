using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class TareaCoberturaSkill
{
    public TareaCoberturaSkill(int idTarea, int idSkill, string nombreSkill, string? categoriaSkill, string tituloTarea, decimal horasEstimadas, DateTime deadline, int idProyecto, string nombreProyecto, string nombreCliente)
    {
        IdTarea = idTarea;
        IdSkill = idSkill;
        NombreSkill = nombreSkill;
        CategoriaSkill = categoriaSkill;
        TituloTarea = tituloTarea;
        HorasEstimadas = horasEstimadas;
        Deadline = deadline;
        IdProyecto = idProyecto;
        NombreProyecto = nombreProyecto;
        NombreCliente = nombreCliente;
    }

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty };

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value, Titulo = Tarea.Titulo, HorasEstimadas = Tarea.HorasEstimadas, Deadline = Tarea.Deadline };
    }
    [JsonIgnore, ReferenciaInterna]
    public Skill Skill { get; set; } = new() { Nombre = string.Empty };

    public int IdSkill
    {
        get => Skill.ID;
        set => Skill = Skill.ID == value ? Skill : new() { ID = value, Nombre = Skill.Nombre, Categoria = Skill.Categoria };
    }
    public string NombreSkill
    {
        get => Skill.Nombre;
        set => Skill.Nombre = value;
    }
    public string? CategoriaSkill
    {
        get => Skill.Categoria;
        set => Skill.Categoria = value;
    }
    public string TituloTarea
    {
        get => Tarea.Titulo;
        set => Tarea.Titulo = value;
    }
    public decimal HorasEstimadas
    {
        get => Tarea.HorasEstimadas;
        set => Tarea.HorasEstimadas = value;
    }
    public DateTime Deadline
    {
        get => Tarea.Deadline ?? default;
        set => Tarea.Deadline = value;
    }
    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new() { Nombre = string.Empty };

    public int IdProyecto
    {
        get => Proyecto.ID;
        set => Proyecto = Proyecto.ID == value ? Proyecto : new() { ID = value, Nombre = Proyecto.Nombre, Cliente = Proyecto.Cliente };
    }
    public string NombreProyecto
    {
        get => Proyecto.Nombre;
        set => Proyecto.Nombre = value;
    }
    public string NombreCliente
    {
        get => Proyecto.NombreCliente ?? string.Empty;
        set => Proyecto.NombreCliente = value;
    }
}
