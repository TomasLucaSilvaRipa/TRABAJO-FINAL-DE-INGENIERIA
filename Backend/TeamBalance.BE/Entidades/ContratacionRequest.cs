using System;
using System.Collections.Generic;
using System.Text;

namespace TeamBalance.BE.Entidades
{
    public class ContratacionRequest
    {
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

        public string Periodicidad { get; set; } = string.Empty;
    }
}
