using System;
using System.Collections.Generic;

namespace TeamBalance.BE.Entidades;

public partial class ContratacionServicio
{
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

    public virtual Agencia? IdAgenciaNavigation { get; set; }

    public virtual PlanComercial IdPlanComercialNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<OperacionPago> OperacionPagos { get; set; } = new List<OperacionPago>();
}
