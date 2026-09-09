using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Bitacora
{
    public Bitacora() { }
    public Bitacora(int? idUsuario, int? idAgencia, string? entidad, int? idEntidad, string accion, string mensaje, string? resultado, string? criticidad, string? modulo)
    {
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Entidad = entidad;
        IdEntidad = idEntidad;
        Accion = accion;
        Mensaje = mensaje;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
        FechaHora = DateTime.Now;
    }

    public Bitacora(int id, int? idUsuario, int? idAgencia, string? entidad, int? idEntidad, string accion, string mensaje, string? resultado, string? criticidad, string? modulo, DateTime fechaHora, string? direccionIP)
    {
        ID = id;
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Entidad = entidad;
        IdEntidad = idEntidad;
        Accion = accion;
        Mensaje = mensaje;
        Resultado = resultado;
        Criticidad = criticidad;
        Modulo = modulo;
        FechaHora = fechaHora;
        DireccionIP = direccionIP;
    }
    public int ID { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdAgencia { get; set; }

    public string? Entidad { get; set; }

    public int? IdEntidad { get; set; }

    public string Accion { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string? Resultado { get; set; }

    public string? Criticidad { get; set; }

    public string? Modulo { get; set; }

    public DateTime FechaHora { get; set; }

    public string? DireccionIP { get; set; }
}
