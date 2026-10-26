namespace TeamBalance.BE.Entidades;
public class TareaRiesgoRetraso
{
    public TareaRiesgoRetraso()
    {
    }

    public TareaRiesgoRetraso(int idTarea, string titulo, string estado, bool bloqueada, DateTime? fechaInicio, DateTime? deadline, decimal horasEstimadas, decimal porcentajeAvance, decimal horasRestantes)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        Estado = estado;
        Bloqueada = bloqueada;
        FechaInicio = fechaInicio;
        Deadline = deadline;
        HorasEstimadas = horasEstimadas;
        PorcentajeAvance = porcentajeAvance;
        HorasRestantes = horasRestantes;
    }

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool Bloqueada { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public decimal HorasRestantes { get; set; }
}
