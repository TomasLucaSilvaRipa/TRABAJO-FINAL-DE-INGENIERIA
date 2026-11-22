using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class OcupacionEmpleadoProyecto
{
    [JsonIgnore, ReferenciaInterna]
    public Empleado Empleado { get; set; } = new() { Nombre = string.Empty };

    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value, Nombre = Empleado.Nombre };
    }
    public string NombreEmpleado
    {
        get => Empleado.Nombre;
        set => Empleado.Nombre = value;
    }
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeSobreCargaProyecto { get; set; }
}
