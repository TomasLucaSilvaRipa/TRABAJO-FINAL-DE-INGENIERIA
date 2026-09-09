using System;
using System.Collections.Generic;
using System.Text;

namespace TeamBalance.BE.Entidades
{
    public class ContratacionRequest
    {
    public ContratacionRequest()
    {
    }

    public ContratacionRequest(string nombreComercialAgencia, string razonSocial, string cUIT, string condicionFiscal, string emailFacturacion, string telefonoContacto, string nombreResponsable, string apellidoResponsable, string emailLaboralResponsable, string cargoResponsable, string proveedorPagoSeleccionado, int idPlanComercial)
    {
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
        IdPlanComercial = idPlanComercial;
    }

        public string NombreComercialAgencia { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string CUIT { get; set; } = string.Empty;
        public string CondicionFiscal { get; set; } = string.Empty;
        public string EmailFacturacion { get; set; } = string.Empty;
        public string TelefonoContacto { get; set; } = string.Empty;

        public string NombreResponsable { get; set; } = string.Empty;
        public string ApellidoResponsable { get; set; } = string.Empty;
        public string EmailLaboralResponsable { get; set; } = string.Empty;
        public string CargoResponsable { get; set; } = string.Empty;

        public string ProveedorPagoSeleccionado { get; set; } = string.Empty;

        public int IdPlanComercial { get; set; }
    }
}
