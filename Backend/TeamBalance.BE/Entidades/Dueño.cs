using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Dueño
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
