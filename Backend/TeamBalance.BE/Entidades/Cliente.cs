using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Cliente
{
    public int ID { get; set; }

    public string Nombre { get; set; } = null!;

    public string? RazonSocial { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual ICollection<AgenciaCliente> AgenciaClientes { get; set; } = new List<AgenciaCliente>();

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
