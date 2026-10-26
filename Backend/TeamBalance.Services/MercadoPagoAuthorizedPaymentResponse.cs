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
    public sealed class MercadoPagoAuthorizedPaymentResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("payment")]
        public MercadoPagoAuthorizedPaymentDetail? Payment { get; set; }
    }
}
