namespace TeamBalance.BE.Entidades;
public class TareaRiesgoRetraso
{
    public TareaRiesgoRetraso(Tarea tarea, decimal horasEstimadas, decimal porcentajeAvance, decimal horasRestantes)
    {
        Tarea = tarea;
        HorasEstimadas = horasEstimadas;
        PorcentajeAvance = porcentajeAvance;
        HorasRestantes = horasRestantes;
    }

    public Tarea Tarea { get; }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeAvance { get; set; }
    public decimal HorasRestantes { get; set; }
}
