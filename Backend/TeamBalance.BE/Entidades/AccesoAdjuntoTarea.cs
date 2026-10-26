namespace TeamBalance.BE.Entidades;

public class AccesoAdjuntoTarea
{
    public Tarea Tarea { get; set; } = new();
    public Usuario Usuario { get; set; } = new();
    public bool RequiereEscritura { get; set; }
}
