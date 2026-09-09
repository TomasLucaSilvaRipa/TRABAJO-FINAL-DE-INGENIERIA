using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class RecomendacionBestFit
{
    public RecomendacionBestFit()
    {
    }

    public RecomendacionBestFit(int iD, int idTarea, int idEmpleadoSugerido, int idUsuarioSolicitante, decimal puntajeCompatibilidad, DateTime fechaGeneracion, bool seleccionadaPorPM, string estado, bool activo)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleadoSugerido = idEmpleadoSugerido;
        IdUsuarioSolicitante = idUsuarioSolicitante;
        PuntajeCompatibilidad = puntajeCompatibilidad;
        FechaGeneracion = fechaGeneracion;
        SeleccionadaPorPM = seleccionadaPorPM;
        Estado = estado;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleadoSugerido { get; set; }

    public int IdUsuarioSolicitante { get; set; }

    public decimal PuntajeCompatibilidad { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public bool SeleccionadaPorPM { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }
}
