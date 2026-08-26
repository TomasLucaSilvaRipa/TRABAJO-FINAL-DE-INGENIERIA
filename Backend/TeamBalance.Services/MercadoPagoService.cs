using System;
using System.Collections.Generic;
using System.Text;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services
{
    public class MercadoPagoService
    {
        public async Task<string> CrearPago(ContratacionServicio contratacion)
        {
            

            return "https://api.mercadopago.com/checkout/preferences";
        }
    }
}
