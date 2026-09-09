using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class OperacionPago
{
    public OperacionPago()
    {
    }

    public OperacionPago(int iD, int idContratacionServicio, string referenciaInterna, string? referenciaProveedor, string proveedor, decimal importe, string moneda, string estado, DateTime fechaCreacion, DateTime? fechaActualizacion, DateTime? fechaAprobacion)
    {
        ID = iD;
        IdContratacionServicio = idContratacionServicio;
        ReferenciaInterna = referenciaInterna;
        ReferenciaProveedor = referenciaProveedor;
        Proveedor = proveedor;
        Importe = importe;
        Moneda = moneda;
        Estado = estado;
        FechaCreacion = fechaCreacion;
        FechaActualizacion = fechaActualizacion;
        FechaAprobacion = fechaAprobacion;
    }

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
}
