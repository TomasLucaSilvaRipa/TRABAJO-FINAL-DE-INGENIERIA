using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Permiso
{
    public Permiso()
    {
    }

    public Permiso(int iD, string nombre, string? descripcion, bool activo, string? codigo, string? url)
    {
        ID = iD;
        Nombre = nombre;
        Descripcion = descripcion;
        Activo = activo;
        Codigo = codigo;
        Url = url;
    }

    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public string? Codigo { get; set; }

    public string? Url { get; set; }
}
