namespace TeamBalance.BE.Entidades;
public class DetalleOcupacionEmpleado
{
    public OcupacionEmpleado Empleado { get; set; } = new OcupacionEmpleado();
    public List<DetalleOcupacionTarea> Tareas { get; set; } = new List<DetalleOcupacionTarea>();
}
