using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PM : Usuario
{
    public PM()
    {
        Proyectos = new List<Proyecto>();
    }

    public PM(int id, string nombre, string email, bool activo, bool autorizadoGestionRecursos, bool puedeExportarLegajos)
    {
        ID = id;
        Nombre = nombre;
        Email = email;
        Activo = activo;
        AutorizadoGestionRecursos = autorizadoGestionRecursos;
        PuedeExportarLegajos = puedeExportarLegajos;
        Proyectos = new List<Proyecto>();
    }

    public bool AutorizadoGestionRecursos { get; set; }

    public bool PuedeExportarLegajos { get; set; }

    public List<Proyecto> Proyectos { get; set; }
}
