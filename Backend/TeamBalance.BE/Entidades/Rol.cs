using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Rol
{
    public Rol()
    {
        Permisos = new List<Permiso>();
    }

    public Rol(int id, string nombre, string? descripcion, bool esRolBase, bool activo, List<Permiso> permisos)
    {
        ID = id;
        Nombre = nombre;
        Descripcion = descripcion;
        EsRolBase = esRolBase;
        Activo = activo;
        Permisos = permisos;
    }

    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool EsRolBase { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Permiso> Permisos { get; set; }
}
