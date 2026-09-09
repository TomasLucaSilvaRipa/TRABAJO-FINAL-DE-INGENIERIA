using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class ConsultaSoporte
{
    public ConsultaSoporte()
    {
    }

    public ConsultaSoporte(int iD, int idUsuario, int? idAgencia, string asunto, string descripcion, string? adjuntosJson, string estado, DateTime fechaCreacion, DateTime? fechaActualizacion, bool activo, DateTime? fechaBaja)
    {
        ID = iD;
        IdUsuario = idUsuario;
        IdAgencia = idAgencia;
        Asunto = asunto;
        Descripcion = descripcion;
        AdjuntosJson = adjuntosJson;
        Estado = estado;
        FechaCreacion = fechaCreacion;
        FechaActualizacion = fechaActualizacion;
        Activo = activo;
        FechaBaja = fechaBaja;
    }

    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public int? IdAgencia { get; set; }

    public string Asunto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? AdjuntosJson { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }
}
