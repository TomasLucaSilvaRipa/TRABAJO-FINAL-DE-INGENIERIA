using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;
public class EmpleadoDisponibilidadOpcion
{
    public EmpleadoDisponibilidadOpcion()
    {
    }

    public EmpleadoDisponibilidadOpcion(int idUsuario, string nombreCompleto)
    {
        IdUsuario = idUsuario;
        NombreCompleto = nombreCompleto;
    }

    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
