using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AusenciaEmpleado
{
    public AusenciaEmpleado()
    {
    }

    public AusenciaEmpleado(int iD, int idEmpleado, string tipoPeriodo, DateTime fechaInicioSolicitada, DateTime fechaFinSolicitada, DateTime? fechaInicioAprobada, DateTime? fechaFinAprobada, decimal? horasNoDisponiblesSolicitadas, decimal? horasNoDisponiblesAprobadas, string? motivo, int? idUsuarioResolucion, string estado, DateTime fechaSolicitud, DateTime? fechaResolucion, string? motivoResolucion, bool activo)
    {
        ID = iD;
        IdEmpleado = idEmpleado;
        TipoPeriodo = tipoPeriodo;
        FechaInicioSolicitada = fechaInicioSolicitada;
        FechaFinSolicitada = fechaFinSolicitada;
        FechaInicioAprobada = fechaInicioAprobada;
        FechaFinAprobada = fechaFinAprobada;
        HorasNoDisponiblesSolicitadas = horasNoDisponiblesSolicitadas;
        HorasNoDisponiblesAprobadas = horasNoDisponiblesAprobadas;
        Motivo = motivo;
        IdUsuarioResolucion = idUsuarioResolucion;
        Estado = estado;
        FechaSolicitud = fechaSolicitud;
        FechaResolucion = fechaResolucion;
        MotivoResolucion = motivoResolucion;
        Activo = activo;
    }

    public int ID { get; set; }

    public int IdEmpleado { get; set; }

    public string TipoPeriodo { get; set; } = null!;

    public DateTime FechaInicioSolicitada { get; set; }

    public DateTime FechaFinSolicitada { get; set; }

    public DateTime? FechaInicioAprobada { get; set; }

    public DateTime? FechaFinAprobada { get; set; }

    public decimal? HorasNoDisponiblesSolicitadas { get; set; }

    public decimal? HorasNoDisponiblesAprobadas { get; set; }

    public string? Motivo { get; set; }

    public int? IdUsuarioResolucion { get; set; }

    public string? Estado { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public string? MotivoResolucion { get; set; }

    public bool Activo { get; set; }

    public string? ComprobanteUrl { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? NombreEmpleado { get; set; }
}

public class TareaDisponibilidadAfectada
{
    public TareaDisponibilidadAfectada()
    {
    }

    public TareaDisponibilidadAfectada(int idTarea, string titulo, string nombreProyecto, DateTime? deadline)
    {
        IdTarea = idTarea;
        Titulo = titulo;
        NombreProyecto = nombreProyecto;
        Deadline = deadline;
    }

    public int IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
}

public class ResultadoGestionDisponibilidad
{
    public ResultadoGestionDisponibilidad()
    {
    }

    public ResultadoGestionDisponibilidad(AusenciaEmpleado ausencia, List<TareaDisponibilidadAfectada> tareasAfectadas)
    {
        Ausencia = ausencia;
        TareasAfectadas = tareasAfectadas;
    }

    public AusenciaEmpleado Ausencia { get; set; } = new AusenciaEmpleado();
    public List<TareaDisponibilidadAfectada> TareasAfectadas { get; set; } = new List<TareaDisponibilidadAfectada>();
}

public class ResolucionAusenciaEmpleado
{
    public int IdAusencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EsParcial { get; set; }
    public DateTime? FechaInicioAprobada { get; set; }
    public DateTime? FechaFinAprobada { get; set; }
    public decimal? HorasNoDisponiblesAprobadas { get; set; }
    public string? MotivoResolucion { get; set; }
}

public class EmpleadoDisponibilidadOpcion
{
    public EmpleadoDisponibilidadOpcion()
    {
    }

    public EmpleadoDisponibilidadOpcion(int idUsuario, string nombreCompleto)
    {
        IdUsuario = idUsuario;
        NombreCompleto = nombreCompleto;
    }

    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class ConsultaCalendarioEquipo
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdEmpleado { get; set; }
}

public class EventoCalendarioEquipo
{
    public int IdEmpleado { get; set; }
    public int IdUsuario { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public decimal HorasSemanales { get; set; }
    public int? IdAusencia { get; set; }
    public string? TipoPeriodo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? HorasNoDisponibles { get; set; }
    public string? Estado { get; set; }
    public string? Motivo { get; set; }
    public string? MotivoResolucion { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public string? TareasAfectadas { get; set; }
}
