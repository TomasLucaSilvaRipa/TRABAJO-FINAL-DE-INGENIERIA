namespace TeamBalance.BE.Entidades;

public class TareaResumenCorreo
{
    public Tarea Tarea { get; set; } = new() { Titulo = string.Empty, Estado = string.Empty };
    public Proyecto ProyectoRelacionado { get; set; } = new() { Nombre = string.Empty };
    public string? TokenEnProgreso { get; set; }
    public string? TokenFinalizada { get; set; }
}
