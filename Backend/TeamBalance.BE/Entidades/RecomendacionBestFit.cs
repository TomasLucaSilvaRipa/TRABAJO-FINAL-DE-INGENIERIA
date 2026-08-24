using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class RecomendacionBestFit
{
    public int ID { get; set; }

    public int IdTarea { get; set; }

    public int IdEmpleadoSugerido { get; set; }

    public int IdUsuarioSolicitante { get; set; }

    public decimal PuntajeCompatibilidad { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public bool SeleccionadaPorPM { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual Empleado IdEmpleadoSugeridoNavigation { get; set; } = null!;

    public virtual Tarea IdTareaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioSolicitanteNavigation { get; set; } = null!;
}
