namespace TeamBalance.BE.Entidades;

public class GestionTareaOpciones
{
    public GestionTareaOpciones(List<Proyecto> proyectos, List<EstadoTarea> estados, List<Usuario> empleados, List<Skill> skills)
    {
        Proyectos = proyectos;
        Estados = estados;
        Empleados = empleados;
        Skills = skills;
    }
    public List<Proyecto> Proyectos { get; set; }
    public List<EstadoTarea> Estados { get; set; }
    public List<Usuario> Empleados { get; set; }
    public List<Skill> Skills { get; set; }
}
