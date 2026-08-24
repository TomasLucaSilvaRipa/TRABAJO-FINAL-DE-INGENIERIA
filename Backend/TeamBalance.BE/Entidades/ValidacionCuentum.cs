using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class ValidacionCuentum
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public string Metodo { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Utilizado { get; set; }

    public DateTime? FechaUtilizacion { get; set; }

    public bool Activo { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
