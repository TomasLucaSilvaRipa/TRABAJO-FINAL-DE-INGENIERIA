using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class ConsultaCalendarioEquipo
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto? Proyecto { get; set; }

    public int? IdProyecto
    {
        get => Proyecto?.ID;
        set => Proyecto = value.HasValue ? (Proyecto?.ID == value.Value ? Proyecto : new() { ID = value.Value }) : null;
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public Empleado? Empleado { get; set; }

    public int? IdEmpleado
    {
        get => Empleado?.ID;
        set => Empleado = value.HasValue ? (Empleado?.ID == value.Value ? Empleado : new() { ID = value.Value }) : null;
    }
}
