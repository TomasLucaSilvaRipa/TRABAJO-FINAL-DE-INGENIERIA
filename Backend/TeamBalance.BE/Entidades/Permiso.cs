using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Permiso
{
    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}
