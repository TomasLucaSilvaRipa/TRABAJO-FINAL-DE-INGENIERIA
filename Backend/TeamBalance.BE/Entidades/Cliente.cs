using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Cliente : Usuario
{
    public Cliente() { }

    public Cliente(int id, string nombre, string? razonSocial, string email, string? telefono, bool activo, List<Proyecto> proyectos)
    {
        ID = id;
        Nombre = nombre;
        RazonSocial = razonSocial;
        Email = email;
        Telefono = telefono;
        Activo = activo;
        Proyectos = proyectos;
    }


    public string? RazonSocial { get; set; }


    public string? Telefono { get; set; }



    public List<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
