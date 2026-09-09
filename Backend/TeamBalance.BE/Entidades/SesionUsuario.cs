using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class SesionUsuario
{
    public SesionUsuario()
    {
    }

    public SesionUsuario(int iD, int idUsuario, string tokenHash, DateTime fechaInicio, DateTime? fechaUltimaActividad, DateTime fechaExpiracion, string? direccionIP, bool activa, DateTime? fechaCierre)
    {
        ID = iD;
        IdUsuario = idUsuario;
        TokenHash = tokenHash;
        FechaInicio = fechaInicio;
        FechaUltimaActividad = fechaUltimaActividad;
        FechaExpiracion = fechaExpiracion;
        DireccionIP = direccionIP;
        Activa = activa;
        FechaCierre = fechaCierre;
    }

    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaUltimaActividad { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public string? DireccionIP { get; set; }

    public bool Activa { get; set; }

    public DateTime? FechaCierre { get; set; }
}
