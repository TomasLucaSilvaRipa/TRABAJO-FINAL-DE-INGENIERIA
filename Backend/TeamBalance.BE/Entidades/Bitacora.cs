using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class Bitacora
{
    public int ID { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdAgencia { get; set; }

    public string? Entidad { get; set; }

    public int? IdEntidad { get; set; }

    public string Accion { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string? Resultado { get; set; }

    public string? Criticidad { get; set; }

    public string? Modulo { get; set; }

    public DateTime FechaHora { get; set; }

    public string? DireccionIP { get; set; }

    public virtual Agencia? IdAgenciaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
