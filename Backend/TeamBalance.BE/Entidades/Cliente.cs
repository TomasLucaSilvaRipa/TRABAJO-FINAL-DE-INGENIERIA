using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Cliente
{
    public Cliente() { }

    public Cliente(int id, int? idAgencia, string nombre, string? razonSocial, string? email, string? telefono, bool activo, DateTime? fechaBaja, List<Proyecto> proyectos)
    {
        ID = id;
        IdAgencia = idAgencia;
        Nombre = nombre;
        RazonSocial = razonSocial;
        Email = email;
        Telefono = telefono;
        Activo = activo;
        FechaBaja = fechaBaja;
        Proyectos = proyectos;
    }
    public int ID { get; set; }
    public int? IdAgencia { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public string? RazonSocial { get; set; }


    public string? Telefono { get; set; }

    public string? Email { get; set; }
    public bool Activo { get; set; }
    public DateTime? FechaBaja { get; set; }



    public List<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
