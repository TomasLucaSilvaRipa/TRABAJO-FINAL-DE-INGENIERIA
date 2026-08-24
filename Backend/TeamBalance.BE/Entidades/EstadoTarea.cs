using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class EstadoTarea
{
    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsBase { get; set; }

    public bool EsFinal { get; set; }

    public bool Activo { get; set; }

    public virtual Agencia IdAgenciaNavigation { get; set; } = null!;

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
