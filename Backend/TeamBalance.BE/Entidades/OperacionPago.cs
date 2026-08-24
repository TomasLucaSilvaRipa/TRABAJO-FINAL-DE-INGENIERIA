using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class OperacionPago
{
    public int ID { get; set; }

    public int IdContratacionServicio { get; set; }

    public string ReferenciaInterna { get; set; } = null!;

    public string? ReferenciaProveedor { get; set; }

    public string Proveedor { get; set; } = null!;

    public decimal Importe { get; set; }

    public string Moneda { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public virtual ContratacionServicio IdContratacionServicioNavigation { get; set; } = null!;
}
