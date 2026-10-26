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
    public sealed class MercadoPagoAutoRecurringRequest
    {
        public MercadoPagoAutoRecurringRequest(int frequency, string frequencyType, decimal transactionAmount, string currencyId)
        {
            Frequency = frequency;
            FrequencyType = frequencyType;
            TransactionAmount = transactionAmount;
            CurrencyId = currencyId;
        }

        [JsonPropertyName("frequency")]
        public int Frequency { get; set; }

        [JsonPropertyName("frequency_type")]
        public string FrequencyType { get; set; } = string.Empty;

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string CurrencyId { get; set; } = string.Empty;
    }
}
