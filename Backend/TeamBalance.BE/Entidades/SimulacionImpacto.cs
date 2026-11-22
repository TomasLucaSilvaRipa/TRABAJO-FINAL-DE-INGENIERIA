using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamBalance.BE.Entidades;

public partial class SimulacionImpacto
{
    [JsonIgnore, ReferenciaInterna]
    public Skill Habilidad { get; set; } = new();

    [JsonIgnore, ReferenciaInterna]
    public Proyecto Proyecto { get; set; } = new();

    public SimulacionImpacto()
    {
    }

    public SimulacionImpacto(int iD, int idTarea, int idEmpleadoCandidato, int idUsuarioCreador, decimal? cargaActual, decimal? cargaProyectada, decimal? disponibilidadRestante, decimal? porcentajeOcupacionActual, decimal? porcentajeOcupacionProyectado, bool generaSobrecarga, string? advertenciasJson, string? impactoOperativo, DateTime fechaCreacion, DateTime? fechaUltimaModificacion, DateTime fechaExpiracion, bool activo)
    {
        ID = iD;
        IdTarea = idTarea;
        IdEmpleadoCandidato = idEmpleadoCandidato;
        IdUsuarioCreador = idUsuarioCreador;
        CargaActual = cargaActual;
        CargaProyectada = cargaProyectada;
        DisponibilidadRestante = disponibilidadRestante;
        PorcentajeOcupacionActual = porcentajeOcupacionActual;
        PorcentajeOcupacionProyectado = porcentajeOcupacionProyectado;
        GeneraSobrecarga = generaSobrecarga;
        AdvertenciasJson = advertenciasJson;
        ImpactoOperativo = impactoOperativo;
        FechaCreacion = fechaCreacion;
        FechaUltimaModificacion = fechaUltimaModificacion;
        FechaExpiracion = fechaExpiracion;
        Activo = activo;
    }

    public SimulacionImpacto(int idTarea, int idEmpleadoCandidato, int idUsuarioCreador, decimal cargaActual, decimal horasTarea, decimal capacidadSemanal, int diasAusenciaProximaSemana, DateTime? deadlineTarea, string? prioridadTarea, string? nombreTarea, string? nombreProyecto)
    {
        IdTarea = idTarea;
        IdEmpleadoCandidato = idEmpleadoCandidato;
        IdUsuarioCreador = idUsuarioCreador;
        CargaActual = cargaActual;
        HorasTarea = horasTarea;
        CapacidadSemanal = capacidadSemanal;
        DiasAusenciaProximaSemana = diasAusenciaProximaSemana;
        DeadlineTarea = deadlineTarea;
        PrioridadTarea = prioridadTarea;
        NombreTarea = nombreTarea;
        NombreProyecto = nombreProyecto;
    }

    public int ID { get; set; }

    [JsonIgnore, ReferenciaInterna]
    public Tarea Tarea { get; set; } = new();

    public int IdTarea
    {
        get => Tarea.ID;
        set => Tarea = Tarea.ID == value ? Tarea : new() { ID = value, Titulo = Tarea.Titulo, Prioridad = Tarea.Prioridad, Deadline = Tarea.Deadline };
    }

    [JsonIgnore, ReferenciaInterna]
    public Empleado EmpleadoCandidato { get; set; } = new();

    public int IdEmpleadoCandidato
    {
        get => EmpleadoCandidato.ID;
        set => EmpleadoCandidato = EmpleadoCandidato.ID == value ? EmpleadoCandidato : new() { ID = value };
    }

    [JsonIgnore, ReferenciaInterna]
    public Usuario UsuarioCreador { get; set; } = new();

    public int IdUsuarioCreador
    {
        get => UsuarioCreador.ID;
        set => UsuarioCreador = UsuarioCreador.ID == value ? UsuarioCreador : new() { ID = value };
    }

    public decimal? CargaActual { get; set; }

    public decimal? CargaProyectada { get; set; }

    public decimal? DisponibilidadRestante { get; set; }

    public decimal? PorcentajeOcupacionActual { get; set; }

    public decimal? PorcentajeOcupacionProyectado { get; set; }

    public bool GeneraSobrecarga { get; set; }

    public string? AdvertenciasJson { get; set; }

    public string? ImpactoOperativo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Activo { get; set; }

    [NotMapped]
    public decimal HorasTarea { get; set; }

    [NotMapped]
    public decimal CapacidadSemanal { get; set; }

    [NotMapped]
    public int DiasAusenciaProximaSemana { get; set; }

    [NotMapped]
    public DateTime? DeadlineTarea
    {
        get => Tarea.Deadline;
        set => Tarea.Deadline = value;
    }

    [NotMapped]
    public string? PrioridadTarea
    {
        get => Tarea.Prioridad;
        set => Tarea.Prioridad = value;
    }

    [NotMapped]
    public string? NombreTarea
    {
        get => Tarea.Titulo;
        set => Tarea.Titulo = value!;
    }

    [NotMapped]
    public string? NombreProyecto
    {
        get => Proyecto.Nombre;
        set => Proyecto.Nombre = value!;
    }

    [NotMapped]
    public string? NombreEmpleado { get; set; }

    [NotMapped]
    public string? NombreSkillRequerida
    {
        get => Habilidad.Nombre;
        set => Habilidad.Nombre = value!;
    }

    [NotMapped]
    public string? NivelSkillCandidato { get; set; }

    [NotMapped]
    public bool TieneSkillRequerida { get; set; }
}
