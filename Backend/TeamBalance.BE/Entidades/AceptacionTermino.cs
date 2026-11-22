using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AceptacionTermino
{
    public AceptacionTermino()
    {
    }

    public AceptacionTermino(int iD, Usuario usuario, TerminosCondicione terminosCondiciones, DateTime fechaAceptacion, string? direccionIP)
    {
        ID = iD;
        Usuario = usuario;
        TerminosCondiciones = terminosCondiciones;
        FechaAceptacion = fechaAceptacion;
        DireccionIP = direccionIP;
    }

    public int ID { get; set; }

    public Usuario Usuario { get; set; } = new();

    public TerminosCondicione TerminosCondiciones { get; set; } = new();

    public DateTime FechaAceptacion { get; set; }

    public string? DireccionIP { get; set; }
}
