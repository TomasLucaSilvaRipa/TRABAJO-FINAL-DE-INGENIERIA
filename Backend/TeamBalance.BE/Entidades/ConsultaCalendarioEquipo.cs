using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class ConsultaCalendarioEquipo
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdEmpleado { get; set; }
}
