namespace TeamBalance.BE.Entidades;
public class DetalleOcupacionTarea
{
    public required Tarea Tarea { get; init; }
    public required Proyecto Proyecto { get; init; }
    public decimal HorasEstimadas { get; set; }
    public decimal IncidenciaSobreCargaEmpleado { get; set; }
}
