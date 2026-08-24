using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class TerminosCondicione
{
    public int ID { get; set; }

    public string Version { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime FechaVigenciaDesde { get; set; }

    public DateTime? FechaVigenciaHasta { get; set; }

    public bool Vigente { get; set; }

    public virtual ICollection<AceptacionTermino> AceptacionTerminos { get; set; } = new List<AceptacionTermino>();
}
