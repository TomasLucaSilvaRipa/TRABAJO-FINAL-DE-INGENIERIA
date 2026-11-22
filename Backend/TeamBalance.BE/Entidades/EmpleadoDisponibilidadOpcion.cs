using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public Usuario Usuario { get; set; } = new();

    public int IdUsuario
    {
        get => Usuario.ID;
        set => Usuario = Usuario.ID == value ? Usuario : new Usuario { ID = value };
    }
    public string NombreCompleto { get; set; } = string.Empty;
}
