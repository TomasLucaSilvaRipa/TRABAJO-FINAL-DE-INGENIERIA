using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Agencia
{
    public Agencia() { }
    public Agencia(int id,string nombreComercial,string razonSocial,string cuit,string emailContacto,string telefonoContacto,DateTime fechaAlta,string estado,bool activo,List<Proyecto> _proyectos, List<Usuario> _usuarios)
    {
        ID = id;
        NombreComercial = nombreComercial;
        RazonSocial = razonSocial;
        CUIT = cuit;
        EmailContacto = emailContacto;
        TelefonoContacto = telefonoContacto;
        FechaAlta = fechaAlta;
        Estado = estado;
        Activo = activo;
        Proyectos = _proyectos;
        Usuarios = _usuarios;
    }
    public int ID { get; set; }

    public string NombreComercial { get; set; } = null!;

    public string? RazonSocial { get; set; }

    public string CUIT { get; set; } = null!;

    public string? CondicionFiscal { get; set; }

    public string EmailContacto { get; set; } = null!;

    public string? TelefonoContacto { get; set; }

    public DateTime FechaAlta { get; set; }

    public string Estado { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }


    public  List<Proyecto> Proyectos { get; set; } = new List<Proyecto>();

    public List<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();

    public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
