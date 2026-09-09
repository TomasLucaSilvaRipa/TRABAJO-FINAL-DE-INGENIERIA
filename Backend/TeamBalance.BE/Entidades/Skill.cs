using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Skill
{
    public Skill()
    {
    }

    public Skill(int iD, string nombre, string? categoria, bool activo)
    {
        ID = iD;
        Nombre = nombre;
        Categoria = categoria;
        Activo = activo;
    }

    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Categoria { get; set; }

    public bool Activo { get; set; }
}
