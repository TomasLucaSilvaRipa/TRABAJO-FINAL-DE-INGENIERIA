using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Rol
{
    public Rol()
    {
        Permisos = new List<Permiso>();
    }

    public Rol(int id, int? idAgencia, string nombre, string? descripcion, string tipoUsuario, bool esRolBase, bool activo, List<Permiso> permisos)
    {
        ID = id;
        IdAgencia = idAgencia;
        Nombre = nombre;
        Descripcion = descripcion;
        TipoUsuario = tipoUsuario;
        EsRolBase = esRolBase;
        Activo = activo;
        Permisos = permisos;
    }

    public int ID { get; set; }

    public int? IdAgencia { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string TipoUsuario { get; set; } = null!;

    public bool EsRolBase { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Permiso> Permisos { get; set; }
}
