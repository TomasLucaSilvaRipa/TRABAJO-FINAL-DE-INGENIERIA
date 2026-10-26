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

    public int IdTarea { get; set; }
    public int IdSkill { get; set; }
    public string NombreSkill { get; set; } = string.Empty;
    public string? CategoriaSkill { get; set; }
    public string TituloTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public DateTime Deadline { get; set; }
    public int IdProyecto { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
}
