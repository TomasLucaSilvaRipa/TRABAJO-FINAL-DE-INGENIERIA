using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class MensajeConsultaSoporte
{
    public int ID { get; set; }

    public int IdConsultaSoporte { get; set; }

    public int IdUsuario { get; set; }

    public string Mensaje { get; set; } = null!;

    public string? AdjuntosJson { get; set; }

    public DateTime Fecha { get; set; }

    public bool Activo { get; set; }

    public virtual ConsultaSoporte IdConsultaSoporteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
