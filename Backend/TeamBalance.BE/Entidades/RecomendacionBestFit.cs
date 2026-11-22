using System.Text.Json.Serialization;
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

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new();

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Empleado EmpleadoSugerido { get; set; } = new() { Seniority = string.Empty };

    public int IdEmpleadoSugerido
    {
        get => EmpleadoSugerido.ID;
        set => EmpleadoSugerido = EmpleadoSugerido.ID == value ? EmpleadoSugerido : new() { ID = value, Seniority = EmpleadoSugerido.Seniority, CantidadTareasHistoricasComparables = EmpleadoSugerido.CantidadTareasHistoricasComparables };
    }

    [JsonIgnore, ReferenciaInterna]
    public Usuario UsuarioSolicitante { get; set; } = new();

    public int IdUsuarioSolicitante
    {
        get => UsuarioSolicitante.ID;
        set => UsuarioSolicitante = UsuarioSolicitante.ID == value ? UsuarioSolicitante : new() { ID = value };
    }

    public decimal PuntajeCompatibilidad { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public bool SeleccionadaPorPM { get; set; }

    public string? Estado { get; set; }

    public bool Activo { get; set; }

    public string NombreEmpleado { get; set; } = string.Empty;

    public string SeniorityEmpleado
    {
        get => EmpleadoSugerido.Seniority;
        set => EmpleadoSugerido.Seniority = value;
    }

    public decimal HorasDisponibles { get; set; }

    public decimal HorasComprometidasSemanales { get; set; }

    public decimal CargaActualPorcentaje { get; set; }

    public decimal MargenHorasSemanales { get; set; }

    public decimal PuntajeSkill { get; set; }

    public decimal PuntajeSeniority { get; set; }

    public decimal PuntajeCapacidad { get; set; }

    public decimal PuntajeCargaActual { get; set; }

    public decimal PuntajeHistorial { get; set; }

    public int CantidadTareasHistoricasComparables
    {
        get => EmpleadoSugerido.CantidadTareasHistoricasComparables;
        set => EmpleadoSugerido.CantidadTareasHistoricasComparables = value;
    }

    public string DetalleCompatibilidad { get; set; } = string.Empty;

    public string Fortalezas { get; set; } = string.Empty;

    public string Advertencias { get; set; } = string.Empty;
}
