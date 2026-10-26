namespace TeamBalance.BE.Entidades;

public class CargaAdjuntoTarea
{
    public Tarea Tarea { get; set; } = new();
    public ArchivoAdjuntoTarea Archivo { get; set; } = new();
    public Stream Contenido { get; set; } = Stream.Null;
}
