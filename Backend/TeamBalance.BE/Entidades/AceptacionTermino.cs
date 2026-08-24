using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AceptacionTermino
{
    public int ID { get; set; }

    public int IdUsuario { get; set; }

    public int IdTerminosCondiciones { get; set; }

    public DateTime FechaAceptacion { get; set; }

    public string? DireccionIP { get; set; }

    public virtual TerminosCondicione IdTerminosCondicionesNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
