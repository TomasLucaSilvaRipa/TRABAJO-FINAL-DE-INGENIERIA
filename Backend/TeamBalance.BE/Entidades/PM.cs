using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PM
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public bool AutorizadoGestionRecursos { get; set; }

    public bool PuedeExportarLegajos { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
