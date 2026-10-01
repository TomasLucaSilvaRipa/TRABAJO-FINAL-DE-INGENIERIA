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

    public RecomendacionBestFit(int iD, int idTarea, int idEmpleadoSugerido, int idUsuarioSolicitante, decimal puntajeCompatibilidad, DateTime fechaGeneracion, bool seleccionadaPorPM, string? estado, bool activo, string nombreEmpleado, string seniorityEmpleado, decimal horasDisponibles)
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
        NombreEmpleado = nombreEmpleado;
        SeniorityEmpleado = seniorityEmpleado;
        HorasDisponibles = horasDisponibles;
    }

    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleadoSugerido { get; set; }

    public int IdUsuarioSolicitante { get; set; }

    public decimal PuntajeCompatibilidad { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public bool SeleccionadaPorPM { get; set; }

    public string? Estado { get; set; }

    public bool Activo { get; set; }

    public string NombreEmpleado { get; set; } = string.Empty;

    public string SeniorityEmpleado { get; set; } = string.Empty;

    public decimal HorasDisponibles { get; set; }
}
