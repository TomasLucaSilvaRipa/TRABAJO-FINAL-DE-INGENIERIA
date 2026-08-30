using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PM:Usuario
{

    public bool AutorizadoGestionRecursos { get; set; }

    public bool PuedeExportarLegajos { get; set; }


    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
