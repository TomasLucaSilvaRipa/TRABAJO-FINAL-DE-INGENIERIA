namespace TeamBalance.BE.Entidades;

public class ConsultaRendimientoEmpleado
{
    public Empleado Empleado { get; set; } = new();
    public int Meses { get; set; } = 6;
}
