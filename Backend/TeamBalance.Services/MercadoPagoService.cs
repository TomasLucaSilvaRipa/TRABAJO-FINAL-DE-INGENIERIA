using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using Microsoft.Extensions.Configuration;


namespace TeamBalance.Services
{
    public class MercadoPagoService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MercadoPagoService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<string> CrearPago(ContratacionPendiente contratacion)
        {
            string? accessToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken)) throw new Exception("No se configuró el Access Token de Mercado Pago.");

            var preference = new Dictionary<string, object?>
            {
                ["items"] = new[]
                {
                    new
                    {
                        title = "Suscripción TeamBalance",
                        quantity = 1,
                        currency_id = contratacion.Moneda,
                        unit_price = contratacion.Importe
                    }
                },
                ["external_reference"] = contratacion.ReferenciaContratacion
            };

            string? publicBaseUrl = _configuration["Frontend:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? publicUri) || publicUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException("Frontend:PublicBaseUrl debe ser una URL pública con HTTPS para recibir el retorno de Mercado Pago.");
                }

                string returnUrl = publicBaseUrl.TrimEnd('/') + "/pago/resultado";
                preference["back_urls"] = new
                {
                    success = returnUrl,
                    pending = returnUrl,
                    failure = returnUrl
                };
                preference["auto_return"] = "approved";
            }

            string? notificationUrl = _configuration["MercadoPago:NotificationUrl"];
            if (!string.IsNullOrWhiteSpace(notificationUrl))
            {
                preference["notification_url"] = notificationUrl;
            }

            using var request = new HttpRequestMessage( HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

            request.Content = JsonContent.Create(preference);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception( $"Mercado Pago rechazó la solicitud: {error}" );
            }

            var resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>();

            if (resultado == null) throw new Exception( "Mercado Pago no devolvió una respuesta válida." );

            // Estamos probando, sandbox.
            return resultado.SandboxInitPoint ?? resultado.InitPoint ?? throw new Exception( "Mercado Pago no devolvió una URL de pago.");
        }

        public async Task<MercadoPagoPayment> ConsultarPago(string paymentId)
        {
            string? accessToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken)) throw new Exception("No se configuró el Access Token de Mercado Pago.");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"No fue posible verificar el pago en Mercado Pago: {error}");
            }

            MercadoPagoPaymentResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>();

            if (resultado is null ||
                resultado.Id <= 0 ||
                string.IsNullOrWhiteSpace(resultado.Status) ||
                string.IsNullOrWhiteSpace(resultado.ExternalReference) ||
                string.IsNullOrWhiteSpace(resultado.CurrencyId))
            {
                throw new InvalidOperationException("Mercado Pago no devolvió los datos necesarios para verificar el pago.");
            }

            return new MercadoPagoPayment(
                resultado.Id.ToString(),
                resultado.Status,
                resultado.ExternalReference,
                resultado.TransactionAmount,
                resultado.CurrencyId);
        }
    }

    public class MercadoPagoPreferenceResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("init_point")]
        public string? InitPoint { get; set; }

        [JsonPropertyName("sandbox_init_point")]
        public string? SandboxInitPoint { get; set; }
    }

    public class MercadoPagoPaymentResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }
    }

    public sealed record MercadoPagoPayment(
        string Id,
        string Status,
        string ExternalReference,
        decimal TransactionAmount,
        string CurrencyId);
}
