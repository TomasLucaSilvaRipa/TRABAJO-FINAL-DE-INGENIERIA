using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AsignacionTarea
{
    public AsignacionTarea()
    {
    }

    public AsignacionTarea(int iD, int idTarea, int idEmpleado, int idUsuarioAsignador, DateTime fechaDesde, DateTime? fechaHasta, string? motivo, decimal horasAsignadas, bool activa)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleado = idEmpleado;
        IdUsuarioAsignador = idUsuarioAsignador;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        Motivo = motivo;
        HorasAsignadas = horasAsignadas;
        Activa = activa;
    }

    public AsignacionTarea(Tarea tarea)
        : this(0, tarea.ID, tarea.IdEmpleadoAsignado ?? 0, 0, DateTime.MinValue, null, null, tarea.HorasEstimadas, true)
    {
        Tarea = tarea;
    }

    public int ID { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Tarea Tarea { get; set; } = new();

    // Adaptador para el formato plano de entrada y salida.
    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value };
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Empleado Empleado { get; set; } = new();

    // Adaptador para el formato plano de entrada y salida.
    public int IdEmpleado
    {
        get => Empleado.ID;
        set => Empleado = Empleado.ID == value ? Empleado : new() { ID = value };
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Usuario UsuarioAsignador { get; set; } = new();

    // Adaptador para el formato plano de entrada y salida.
    public int IdUsuarioAsignador
    {
        get => UsuarioAsignador.ID;
        set => UsuarioAsignador = UsuarioAsignador.ID == value ? UsuarioAsignador : new() { ID = value };
    }

    public DateTime FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? Motivo { get; set; }

    // Horas de la tarea que forman parte de la carga planificada de este empleado.
    public decimal HorasAsignadas { get; set; }

    public bool Activa { get; set; }

    // Campos de compatibilidad con respuestas anteriores. Las referencias internas
    // se excluyen del JSON porque pueden apuntar a la tarea o al empleado que contiene esta asignación.
    public Empleado? IdEmpleadoNavigation => null;

    public Tarea? IdTareaNavigation => null;

    public Usuario? IdUsuarioAsignadorNavigation => null;
}
