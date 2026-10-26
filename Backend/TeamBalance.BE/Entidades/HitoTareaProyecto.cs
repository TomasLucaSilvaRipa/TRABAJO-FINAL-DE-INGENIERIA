namespace TeamBalance.BE.Entidades;
public class HitoTareaProyecto
{
    public HitoTareaProyecto()
    {
    }

    public HitoTareaProyecto(int idTarea, string titulo, DateTime? fechaInicio, DateTime? deadline, decimal horasEstimadas, decimal porcentajeAvance, decimal horasRestantes)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        HorasEstimadas = horasEstimadas;
        PorcentajeAvance = porcentajeAvance;
        HorasRestantes = horasRestantes;
    }

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public decimal HorasRestantes { get; set; }
}
