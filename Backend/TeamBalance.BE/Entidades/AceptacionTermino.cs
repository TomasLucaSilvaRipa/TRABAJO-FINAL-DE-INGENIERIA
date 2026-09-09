using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AceptacionTermino
{
    public AceptacionTermino()
    {
    }

    public AceptacionTermino(int iD, int idUsuario, int idTerminosCondiciones, DateTime fechaAceptacion, string? direccionIP)
    {
        ID = iD;
        IdUsuario = idUsuario;
        IdTerminosCondiciones = idTerminosCondiciones;
        FechaAceptacion = fechaAceptacion;
        DireccionIP = direccionIP;
    }

    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public int IdTerminosCondiciones { get; set; }

    public DateTime FechaAceptacion { get; set; }

    public string? DireccionIP { get; set; }
}
