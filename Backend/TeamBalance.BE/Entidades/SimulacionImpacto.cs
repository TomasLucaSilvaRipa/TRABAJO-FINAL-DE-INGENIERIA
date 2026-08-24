using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class SimulacionImpacto
{
    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleadoCandidato { get; set; }

    public int IdUsuarioCreador { get; set; }

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

    public virtual Empleado IdEmpleadoCandidatoNavigation { get; set; } = null!;

    public virtual Tarea IdTareaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioCreadorNavigation { get; set; } = null!;
}
