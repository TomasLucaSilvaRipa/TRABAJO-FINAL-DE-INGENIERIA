namespace TeamBalance.BE.Entidades;
public class DetalleOcupacionTarea
{
    public int IdTarea { get; set; }
    public int IdProyecto { get; set; }
    public int IdPMResponsable { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal IncidenciaSobreCargaEmpleado { get; set; }
}
