using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using Microsoft.Extensions.Configuration;

namespace TeamBalance.Services
{
    public sealed class MercadoPagoPayment
    {
        public MercadoPagoPayment(string id, string status, string externalReference, decimal transactionAmount, string currencyId)
        {
            Id = id;
            Status = status;
            ExternalReference = externalReference;
            TransactionAmount = transactionAmount;
            CurrencyId = currencyId;
        }

        public string Id { get; set; }
        public string Status { get; set; }
        public string ExternalReference { get; set; }
        public decimal TransactionAmount { get; set; }
        public string CurrencyId { get; set; }
    }
}
