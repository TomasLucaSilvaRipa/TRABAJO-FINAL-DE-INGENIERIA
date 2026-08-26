using System;
using System.Collections.Generic;
using System.Text;
using TeamBalance.BE.Entidades;
using TeamBalance.Services;

namespace TeamBalance.BLL
{
    public class ContratacionBLL
    {
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly StripeService _stripeService;

        public ContratacionBLL( MercadoPagoService mercadoPagoService, StripeService stripeService)
        {
            _mercadoPagoService = mercadoPagoService;
            _stripeService = stripeService;
        }

        public async Task<string> Contratar(ContratacionServicio contratacion)
        {
            if (contratacion == null) { throw new Exception("La contratación es obligatoria."); }

            if (contratacion.ProveedorPagoSeleccionado == "MercadoPago") { return await _mercadoPagoService.CrearPago(contratacion); }

            if (contratacion.ProveedorPagoSeleccionado == "Stripe") { return await _stripeService.CrearPago(contratacion); }

            throw new Exception("Proveedor de pago inválido.");
        }
    }
}
