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
        return await CrearPago(new MercadoPagoPreferenceRequest("Suscripción TeamBalance", contratacion.Importe, contratacion.Moneda, contratacion.ReferenciaContratacion));
    }

    public async Task<string> CrearPago(MercadoPagoPreferenceRequest pago)
        {
            string? accessToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken)) throw new Exception("No se configuró el Access Token de Mercado Pago.");

        if (CobroPruebaHabilitado())
        {
            (decimal importePrueba, string monedaPrueba) = ObtenerCobroPrueba(pago.CodigoPlan);
            pago = new MercadoPagoPreferenceRequest(pago.Titulo, importePrueba, monedaPrueba, pago.ReferenciaExterna, pago.CodigoPlan);
        }

            Dictionary<string, object?> preference = new Dictionary<string, object?>
            {
                ["items"] = new[]
                {
                    new
                    {
                        title = pago.Titulo,
                        quantity = 1,
                        currency_id = pago.Moneda,
                        unit_price = pago.Importe
                    }
                },
                ["external_reference"] = pago.ReferenciaExterna
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

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");

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

            MercadoPagoPreferenceResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>();

            if (resultado == null) throw new Exception( "Mercado Pago no devolvió una respuesta válida." );

            // Estamos probando, sandbox.
            return resultado.SandboxInitPoint ?? resultado.InitPoint ?? throw new Exception( "Mercado Pago no devolvió una URL de pago.");
        }

    public bool EsCobroPruebaEsperado(MercadoPagoPayment pago, string? codigoPlan)
    {
        if (!CobroPruebaHabilitado()) return false;

        (decimal importeEsperado, string monedaEsperada) = ObtenerCobroPrueba(codigoPlan);
        return pago.TransactionAmount == importeEsperado &&
            string.Equals(pago.CurrencyId, monedaEsperada, StringComparison.OrdinalIgnoreCase);
    }

    public (string PublicKey, decimal Importe, string Moneda) ObtenerCobroRecurrente(string? codigoPlan)
    {
        string publicKey = _configuration["MercadoPago:PublicKey"] ?? throw new InvalidOperationException("No se configuró la Public Key de Mercado Pago para autorizar renovaciones automáticas.");
        (decimal importe, string moneda) = ObtenerCobroPrueba(codigoPlan);
        return (publicKey, importe, moneda);
    }

    public async Task<MercadoPagoPreapproval> CrearRenovacionAutomatica(string nombrePlan, string referenciaExterna, string payerEmail, string cardToken)
    {
        if (string.IsNullOrWhiteSpace(cardToken) || string.IsNullOrWhiteSpace(payerEmail)) throw new ArgumentException("Completá el medio de pago y el email de la cuenta de prueba.");
        string? accessToken = _configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
        (decimal importe, string moneda) = ObtenerCobroPrueba(nombrePlan);
        var body = new
        {
            reason = $"TeamBalance · renovación {nombrePlan}", external_reference = referenciaExterna, payer_email = payerEmail,
            card_token_id = cardToken, status = "authorized",
            auto_recurring = new { frequency = 1, frequency_type = "months", transaction_amount = importe, currency_id = moneda }
        };
        using HttpRequestMessage request = new(HttpMethod.Post, "https://api.mercadopago.com/preapproval");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (CobroPruebaHabilitado()) request.Headers.TryAddWithoutValidation("X-scope", "stage");
        request.Content = JsonContent.Create(body);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago no pudo autorizar la renovación: {await response.Content.ReadAsStringAsync()}");
        MercadoPagoPreapprovalResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalResponse>();
        if (resultado is null || string.IsNullOrWhiteSpace(resultado.Id) || string.IsNullOrWhiteSpace(resultado.Status)) throw new InvalidOperationException("Mercado Pago no devolvió la autorización recurrente.");
        return new MercadoPagoPreapproval(resultado.Id, resultado.Status);
    }

    public async Task<List<MercadoPagoAuthorizedPayment>> ConsultarCobrosRecurrentes(string preapprovalId)
    {
        string? accessToken = _configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
        using HttpRequestMessage request = new(HttpMethod.Get, $"https://api.mercadopago.com/authorized_payments/search?preapproval_id={Uri.EscapeDataString(preapprovalId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("No fue posible consultar los cobros recurrentes en Mercado Pago.");
        MercadoPagoAuthorizedPaymentsResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoAuthorizedPaymentsResponse>();
        return resultado?.Results?.Where(x => x.Payment?.Status?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true).Select(x => new MercadoPagoAuthorizedPayment(x.Id.ToString(), x.TransactionAmount, x.CurrencyId ?? "ARS", x.Payment!.Id.ToString())).ToList() ?? new List<MercadoPagoAuthorizedPayment>();
    }

    public async Task<MercadoPagoPreapproval> ActualizarRenovacionAutomatica(string preapprovalId, string estado)
    {
        if (string.IsNullOrWhiteSpace(preapprovalId)) throw new ArgumentException("No existe una autorización recurrente para actualizar.");
        if (estado is not ("authorized" or "paused")) throw new ArgumentException("El estado solicitado para la autorización recurrente no es válido.");

        string? accessToken = _configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");

        using HttpRequestMessage request = new(HttpMethod.Put, $"https://api.mercadopago.com/preapproval/{Uri.EscapeDataString(preapprovalId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (CobroPruebaHabilitado()) request.Headers.TryAddWithoutValidation("X-scope", "stage");
        request.Content = JsonContent.Create(new { status = estado });

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago no pudo actualizar la renovación automática: {await response.Content.ReadAsStringAsync()}");

        MercadoPagoPreapprovalResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalResponse>();
        if (resultado is null || string.IsNullOrWhiteSpace(resultado.Id) || string.IsNullOrWhiteSpace(resultado.Status))
            throw new InvalidOperationException("Mercado Pago no devolvió el estado de la autorización recurrente.");

        return new MercadoPagoPreapproval(resultado.Id, resultado.Status);
    }

    private (decimal Importe, string Moneda) ObtenerCobroPrueba(string? codigoPlan)
    {
        string? importeConfigurado = _configuration[$"MercadoPago:CobroPrueba:ImportesPorPlan:{codigoPlan}"]
            ?? _configuration["MercadoPago:CobroPrueba:ImportePredeterminado"];
        decimal importe = decimal.TryParse(importeConfigurado, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importeLeido)
            ? importeLeido
            : 1000m;
        string moneda = _configuration["MercadoPago:CobroPrueba:Moneda"] ?? "ARS";
        return (importe, moneda);
    }

    private bool CobroPruebaHabilitado() => bool.TryParse(
        _configuration["MercadoPago:CobroPrueba:Habilitado"], out bool habilitado) && habilitado;

        public async Task<MercadoPagoPayment> ConsultarPago(string paymentId)
        {
            string? accessToken = _configuration["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken)) throw new Exception("No se configuró el Access Token de Mercado Pago.");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
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

            MercadoPagoPayment pago = new MercadoPagoPayment(resultado.Id.ToString(), resultado.Status, resultado.ExternalReference, resultado.TransactionAmount, resultado.CurrencyId);
            return pago;
        }
    }

    public class MercadoPagoPreferenceResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("init_point")]
        public string? InitPoint { get; set; }

        [JsonPropertyName("sandbox_init_point")]
        public string? SandboxInitPoint { get; set; }
    }

    public class MercadoPagoPaymentResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string? CurrencyId { get; set; }
    }

    public sealed class MercadoPagoPreapprovalResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public sealed class MercadoPagoAuthorizedPaymentsResponse
    {
        [JsonPropertyName("results")]
        public List<MercadoPagoAuthorizedPaymentResponse>? Results { get; set; }
    }

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

    public sealed class MercadoPagoAuthorizedPaymentDetail
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

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

    public sealed record MercadoPagoPreapproval(string Id, string Status);

    public sealed record MercadoPagoAuthorizedPayment(string Id, decimal TransactionAmount, string CurrencyId, string PaymentId);
}

public sealed class MercadoPagoPreferenceRequest
{
    public MercadoPagoPreferenceRequest(string titulo, decimal importe, string moneda, string referenciaExterna, string? codigoPlan = null)
    {
        Titulo = titulo;
        Importe = importe;
        Moneda = moneda;
        ReferenciaExterna = referenciaExterna;
        CodigoPlan = codigoPlan;
    }

    public string Titulo { get; }
    public decimal Importe { get; }
    public string Moneda { get; }
    public string ReferenciaExterna { get; }
    public string? CodigoPlan { get; }
}
