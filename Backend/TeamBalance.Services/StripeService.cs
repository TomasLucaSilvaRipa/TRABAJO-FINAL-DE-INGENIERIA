using System;
using System.Collections.Generic;
using System.Text;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services
{
    public class StripeService
    {
        public async Task<string> CrearPago(ContratacionServicio contratacion)
        {

            return "https://api.stripe.com/checkout/sessions";
        }
    }
}
