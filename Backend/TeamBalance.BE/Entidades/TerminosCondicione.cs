using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class TerminosCondicione
{
    public TerminosCondicione()
    {
    }

    public TerminosCondicione(int iD, string version, string titulo, string contenido, DateTime fechaVigenciaDesde, DateTime? fechaVigenciaHasta, bool vigente)
    {
        ID = iD;
        Version = version;
        Titulo = titulo;
        Contenido = contenido;
        FechaVigenciaDesde = fechaVigenciaDesde;
        FechaVigenciaHasta = fechaVigenciaHasta;
        Vigente = vigente;
    }

    public int ID { get; set; }

    public string Version { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime FechaVigenciaDesde { get; set; }

    public DateTime? FechaVigenciaHasta { get; set; }

    public bool Vigente { get; set; }
}
