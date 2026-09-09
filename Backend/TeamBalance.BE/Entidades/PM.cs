using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class PM:Usuario
{

    public PM(){}

    public PM(int id, string nombre, string? razonSocial, string? email, string? telefono, bool activo, bool autorizadoGestionRecursos, bool puedeExportarLegajos)
    {
        ID = id;
        Nombre = nombre;
        Email = email;
        Activo = activo;
        AutorizadoGestionRecursos = autorizadoGestionRecursos;
        PuedeExportarLegajos = puedeExportarLegajos;
    }

    public bool AutorizadoGestionRecursos { get; set; }

    public bool PuedeExportarLegajos { get; set; }

    public List<Proyecto> Proyectos { get; set; }
}
