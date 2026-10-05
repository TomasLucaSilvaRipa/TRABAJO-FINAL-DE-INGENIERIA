namespace TeamBalance.BE.Entidades;

public class FiltroTarea
{
    public FiltroTarea()
    {
    }

    public FiltroTarea(int? idProyecto, string? estado, string? prioridad, int? idEmpleadoAsignado, int? idSkillRequerido, DateTime? deadlineDesde, DateTime? deadlineHasta)
    {
        IdProyecto = idProyecto;
        Estado = estado;
        Prioridad = prioridad;
        IdEmpleadoAsignado = idEmpleadoAsignado;
        IdSkillRequerido = idSkillRequerido;
        DeadlineDesde = deadlineDesde;
        DeadlineHasta = deadlineHasta;
    }

    public int? IdProyecto { get; set; }

    public string? Estado { get; set; }

    public string? Prioridad { get; set; }

    public int? IdEmpleadoAsignado { get; set; }

    public int? IdSkillRequerido { get; set; }

    public DateTime? DeadlineDesde { get; set; }

    public DateTime? DeadlineHasta { get; set; }
}
