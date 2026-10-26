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
    public sealed class MercadoPagoAuthorizedPaymentDetail
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
