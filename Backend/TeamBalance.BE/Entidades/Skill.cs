using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Skill
{
    public Skill()
    {
    }

    public Skill(int id, bool activo)
    {
        ID = id;
        Activo = activo;
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

    [JsonIgnore, ReferenciaInterna]
    public AreaSkill? Area { get; set; }

    public int? IdAreaSkill
    {
        get => Area?.ID;
        set
        {
            if (value is int id)
            {
                if (Area?.ID != id)
                    Area = new() { ID = id, Nombre = NombreArea! };
            }
            else
            {
                _nombreAreaSinReferencia = NombreArea;
                Area = null;
            }
        }
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    private string? _nombreAreaSinReferencia;

    public string? NombreArea
    {
        get => Area is null ? _nombreAreaSinReferencia : Area.Nombre;
        set
        {
            if (Area is null)
                _nombreAreaSinReferencia = value;
            else
                Area.Nombre = value!;
        }
    }

    public bool Activo { get; set; }
}
