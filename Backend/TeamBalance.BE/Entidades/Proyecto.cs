using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Proyecto
{
    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public int IdCliente { get; set; }

    public int IdPMResponsable { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? Deadline { get; set; }

    public decimal HorasEstimadasTotales { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Agencia IdAgenciaNavigation { get; set; } = null!;

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual PM IdPMResponsableNavigation { get; set; } = null!;

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
