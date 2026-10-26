using TeamBalance.BE.Entidades;

namespace TeamBalance.BLL;

internal sealed class ContextoProyectoAnalitico
{
    public Proyecto Proyecto { get; set; } = new();
    public List<Tarea> Tareas { get; set; } = new();
    public List<RegistroHora> Registros { get; set; } = new();
    public List<Empleado> Equipo { get; set; } = new();
}
