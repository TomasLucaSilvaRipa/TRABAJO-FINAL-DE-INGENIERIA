using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class AgenciaCliente
{
    public int ID { get; set; }

    public int IdAgencia { get; set; }

    public int IdCliente { get; set; }

    public DateTime FechaAlta { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Agencia IdAgenciaNavigation { get; set; } = null!;

    public virtual Cliente IdClienteNavigation { get; set; } = null!;
}
