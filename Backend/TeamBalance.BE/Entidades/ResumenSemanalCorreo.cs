namespace TeamBalance.BE.Entidades;
public class ResumenSemanalCorreo
{
    public int ProyectosActivos { get; set; }
    public int TareasActivas { get; set; }
    public int TareasVencidas { get; set; }
    public int EmpleadosSobrecargados { get; set; }
    public decimal OcupacionPromedio { get; set; }
}
