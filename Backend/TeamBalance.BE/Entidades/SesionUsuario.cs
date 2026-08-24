using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class SesionUsuario
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaUltimaActividad { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public string? DireccionIP { get; set; }

    public bool Activa { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
