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
    public sealed class MercadoPagoPreapprovalRequest
    {
        public MercadoPagoPreapprovalRequest(string reason, string externalReference, string payerEmail, string backUrl, string status, MercadoPagoAutoRecurringRequest autoRecurring)
        {
            Reason = reason;
            ExternalReference = externalReference;
            PayerEmail = payerEmail;
            BackUrl = backUrl;
            Status = status;
            AutoRecurring = autoRecurring;
        }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("payer_email")]
        public string PayerEmail { get; set; } = string.Empty;

        [JsonPropertyName("back_url")]
        public string BackUrl { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("auto_recurring")]
        public MercadoPagoAutoRecurringRequest AutoRecurring { get; set; }
    }
}
