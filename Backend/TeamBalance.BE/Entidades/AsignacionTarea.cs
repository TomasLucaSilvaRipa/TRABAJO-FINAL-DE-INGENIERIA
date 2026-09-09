using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AsignacionTarea
{
    public AsignacionTarea()
    {
    }

    public AsignacionTarea(int iD, int idTarea, int idEmpleado, int idUsuarioAsignador, DateTime fechaDesde, DateTime? fechaHasta, string? motivo, bool activa)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleado = idEmpleado;
        IdUsuarioAsignador = idUsuarioAsignador;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        Motivo = motivo;
        Activa = activa;
    }

    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleado { get; set; }

    public int IdUsuarioAsignador { get; set; }

    public DateTime FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Motivo { get; set; }

    public bool Activa { get; set; }
}
