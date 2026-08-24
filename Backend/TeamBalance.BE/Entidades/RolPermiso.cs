using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class RolPermiso
{
    public int ID { get; set; }

    public int IdRol { get; set; }

    public int IdPermiso { get; set; }

    public virtual Permiso IdPermisoNavigation { get; set; } = null!;

    public virtual Rol IdRolNavigation { get; set; } = null!;
}
