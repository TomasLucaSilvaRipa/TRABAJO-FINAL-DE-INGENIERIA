using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class ContratacionServicio
{
    public ContratacionServicio()
    {
    }

    public ContratacionServicio(int iD, int? idAgencia, int idPlanComercial, int? idUsuario, string referenciaContratacion, string nombreComercialAgencia, string? razonSocial, string cUIT, string? condicionFiscal, string? emailFacturacion, string? telefonoContacto, string nombreResponsable, string apellidoResponsable, string emailLaboralResponsable, string? cargoResponsable, string? proveedorPagoSeleccionado, string estadoContratacion, DateTime fechaSolicitud, DateTime? fechaRespuesta, string? mensajeRespuesta, bool activo, DateTime? fechaBaja)
    {
        ID = iD;
        IdAgencia = idAgencia;
        IdPlanComercial = idPlanComercial;
        IdUsuario = idUsuario;
        ReferenciaContratacion = referenciaContratacion;
        NombreComercialAgencia = nombreComercialAgencia;
        RazonSocial = razonSocial;
        CUIT = cUIT;
        CondicionFiscal = condicionFiscal;
        EmailFacturacion = emailFacturacion;
        TelefonoContacto = telefonoContacto;
        NombreResponsable = nombreResponsable;
        ApellidoResponsable = apellidoResponsable;
        EmailLaboralResponsable = emailLaboralResponsable;
        CargoResponsable = cargoResponsable;
        ProveedorPagoSeleccionado = proveedorPagoSeleccionado;
        EstadoContratacion = estadoContratacion;
        FechaSolicitud = fechaSolicitud;
        FechaRespuesta = fechaRespuesta;
        MensajeRespuesta = mensajeRespuesta;
        Activo = activo;
        FechaBaja = fechaBaja;
    }

    public int ID { get; set; }

    public int? IdAgencia { get; set; }

    public int IdPlanComercial { get; set; }

    public int? IdUsuario { get; set; }

    public string ReferenciaContratacion { get; set; } = null!;

    public string NombreComercialAgencia { get; set; } = null!;

    public string? RazonSocial { get; set; }

    public string CUIT { get; set; } = null!;

    public string? CondicionFiscal { get; set; }

    public string? EmailFacturacion { get; set; }

    public string? TelefonoContacto { get; set; }

    public string NombreResponsable { get; set; } = null!;

    public string ApellidoResponsable { get; set; } = null!;

    public string EmailLaboralResponsable { get; set; } = null!;

    public string? CargoResponsable { get; set; }

    public string? ProveedorPagoSeleccionado { get; set; }

    public string EstadoContratacion { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    public string? MensajeRespuesta { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }
}
