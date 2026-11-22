namespace TeamBalance.BE.Entidades;
public class DetalleOcupacionEmpleado
{
    public required OcupacionEmpleado Empleado { get; init; }
    public List<DetalleOcupacionTarea> Tareas { get; set; } = new List<DetalleOcupacionTarea>();
}
