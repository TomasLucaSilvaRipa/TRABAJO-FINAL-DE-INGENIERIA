using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class MensajeConsultaSoporte
{
    public MensajeConsultaSoporte()
    {
    }

    public MensajeConsultaSoporte(int iD, int idConsultaSoporte, int idUsuario, string mensaje, string? adjuntosJson, DateTime fecha, bool activo)
    {
        ID = iD;
        IdConsultaSoporte = idConsultaSoporte;
        IdUsuario = idUsuario;
        Mensaje = mensaje;
        AdjuntosJson = adjuntosJson;
        Fecha = fecha;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdConsultaSoporte { get; set; }

    public int IdUsuario { get; set; }

    public string Mensaje { get; set; } = null!;

    public string? AdjuntosJson { get; set; }

    public DateTime Fecha { get; set; }

    public bool Activo { get; set; }
}
