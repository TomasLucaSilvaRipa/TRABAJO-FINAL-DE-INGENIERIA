using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Agencia
{
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


    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();

    public virtual ICollection<Suscripcion> Suscripcions { get; set; } = new List<Suscripcion>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
