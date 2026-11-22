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
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("No se configuró el Access Token de Mercado Pago.");
            if (CobroPruebaHabilitado() && !pago.UsarImporteExacto)
            {
                (decimal importePrueba, string monedaPrueba) = ObtenerCobroPrueba(pago.CodigoPlan);
                pago = new MercadoPagoPreferenceRequest(pago.Titulo, importePrueba, monedaPrueba, pago.ReferenciaExterna, pago.CodigoPlan);
            }

            Dictionary<string, object?> preference = new Dictionary<string, object?>();
            ItemPreferenciaMercadoPago item = new ItemPreferenciaMercadoPago(pago.Titulo, 1, pago.Moneda, pago.Importe);
            List<ItemPreferenciaMercadoPago> items = new List<ItemPreferenciaMercadoPago>();
            items.Add(item);
            preference.Add("items", items);
            preference.Add("external_reference", pago.ReferenciaExterna);
            if (!string.IsNullOrEmpty(pago.MedioPago))
            {
                string[] tipos = { "credit_card", "debit_card", "ticket", "atm", "bank_transfer", "prepaid_card" };
                preference["payment_methods"] = new
                {
                    excluded_payment_types = tipos.Where(tipo => tipo != pago.MedioPago).Select(tipo => new { id = tipo }).ToArray(),
                };
            }
            string? publicBaseUrl = _configuration["Frontend:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? publicUri) || publicUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException("Frontend:PublicBaseUrl debe ser una URL pública con HTTPS para recibir el retorno de Mercado Pago.");
                }

                string returnUrl = publicBaseUrl.TrimEnd('/') + "/pago/resultado";
                preference["back_urls"] = new UrlsRetornoMercadoPago(returnUrl, returnUrl, returnUrl);
                preference["auto_return"] = "approved";
            }

            string? notificationUrl = _configuration["MercadoPago:NotificationUrl"];
            if (!string.IsNullOrWhiteSpace(notificationUrl))
            {
                preference["notification_url"] = notificationUrl;
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrWhiteSpace(pago.ClaveIdempotencia)) request.Headers.TryAddWithoutValidation("X-Idempotency-Key", pago.ClaveIdempotencia);
            request.Content = JsonContent.Create(preference);
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Mercado Pago rechazó la solicitud: {error}");
            }

            MercadoPagoPreferenceResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>();
            if (resultado == null)
                throw new Exception("Mercado Pago no devolvió una respuesta válida.");
            // Estamos probando, sandbox.
            return resultado.SandboxInitPoint ?? resultado.InitPoint ?? throw new Exception("Mercado Pago no devolvió una URL de pago.");
        }

        public bool EsCobroPruebaEsperado(MercadoPagoPayment pago, string? codigoPlan)
        {
            if (!CobroPruebaHabilitado())
                return false;
            (decimal importeEsperado, string monedaEsperada) = ObtenerCobroPrueba(codigoPlan);
            return pago.TransactionAmount == importeEsperado && string.Equals(pago.CurrencyId, monedaEsperada, StringComparison.OrdinalIgnoreCase);
        }

        public (string PublicKey, decimal Importe, string Moneda) ObtenerCobroRecurrente(string? codigoPlan)
        {
            string publicKey = _configuration["MercadoPago:PublicKey"] ?? throw new InvalidOperationException("No se configuró la Public Key de Mercado Pago para autorizar renovaciones automáticas.");
            (decimal importe, string moneda) = ObtenerCobroPrueba(codigoPlan);
            return (publicKey, importe, moneda);
        }

        public CobroMercadoPago ObtenerCobroPlan(PlanComercial plan)
        {
            if (!CobroPruebaHabilitado())
                return new CobroMercadoPago(plan.PrecioVigente, plan.Moneda);
            (decimal importe, string moneda) = ObtenerCobroPrueba(plan.Nombre);
            return new CobroMercadoPago(importe, moneda);
        }

        public async Task<MercadoPagoPreapproval> CrearRenovacionAutomatica(SolicitudRenovacionMercadoPago renovacion)
        {
            if (string.IsNullOrWhiteSpace(renovacion.PayerEmail))
                throw new ArgumentException("Ingresá el email de la cuenta compradora de Mercado Pago.");
            string? accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
            string? publicBaseUrl = _configuration["Frontend:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(publicBaseUrl) || !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? publicUri) || publicUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Frontend:PublicBaseUrl debe ser una URL pública con HTTPS para autorizar renovaciones automáticas.");
            (decimal importe, string moneda) = ObtenerCobroPrueba(renovacion.PlanComercial.Nombre);
            MercadoPagoAutoRecurringRequest autoRecurring = new MercadoPagoAutoRecurringRequest(Math.Max(1, renovacion.PlanComercial.DuracionMeses), "months", importe, moneda, renovacion.FechaPrimerCobro.ToUniversalTime());
            string backUrl = publicBaseUrl.TrimEnd('/') + "/pago/resultado?referencia_renovacion=" + Uri.EscapeDataString(renovacion.ReferenciaExterna) + "&renovacion=mercadopago";
            MercadoPagoPreapprovalRequest body = new MercadoPagoPreapprovalRequest($"TeamBalance · renovación {renovacion.PlanComercial.Nombre}", renovacion.ReferenciaExterna, renovacion.PayerEmail, backUrl, "pending", autoRecurring);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/preapproval");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(body);
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string detalle = await response.Content.ReadAsStringAsync();
                string respuestaProveedor = string.IsNullOrWhiteSpace(detalle) ? $"{(int)response.StatusCode} {response.ReasonPhrase}" : detalle;
                throw new InvalidOperationException($"Mercado Pago no pudo autorizar la renovación: {respuestaProveedor}");
            }

            MercadoPagoPreapprovalResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalResponse>();
            if (resultado is null || string.IsNullOrWhiteSpace(resultado.Id) || string.IsNullOrWhiteSpace(resultado.Status) || string.IsNullOrWhiteSpace(resultado.InitPoint))
                throw new InvalidOperationException("Mercado Pago no devolvió el enlace para autorizar la renovación.");
            var preapproval = CrearPreapproval(resultado, renovacion.ReferenciaExterna);
            if ((preapproval.NextPaymentDate ?? preapproval.StartDate) is not DateTimeOffset fechaProgramada ||
                fechaProgramada.ToUnixTimeSeconds() < renovacion.FechaPrimerCobro.ToUnixTimeSeconds())
                throw new InvalidOperationException("Mercado Pago no confirmó un primer cobro a partir del vencimiento. La renovación no se inició.");

            return preapproval;
        }

        public async Task<MercadoPagoPreapproval> ConsultarRenovacionPorId(string id)
        {
            using var request = CrearSolicitudAutenticada(HttpMethod.Get,
                $"https://api.mercadopago.com/preapproval/{Uri.EscapeDataString(id)}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Mercado Pago no pudo consultar la autorización de renovación.");
            var resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalResponse>();
            if (resultado?.Id != id || string.IsNullOrWhiteSpace(resultado.ExternalReference) || string.IsNullOrWhiteSpace(resultado.Status))
                throw new InvalidOperationException("Mercado Pago no devolvió los datos de la autorización.");
            return CrearPreapproval(resultado, resultado.ExternalReference);
        }

        public async Task<MercadoPagoPreapproval> ConsultarRenovacionAutomatica(string referenciaExterna)
        {
            if (string.IsNullOrWhiteSpace(referenciaExterna))
                throw new ArgumentException("No se informó la referencia de la renovación automática.");
            string? accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
            string url = "https://api.mercadopago.com/preapproval/search?q=" + Uri.EscapeDataString(referenciaExterna);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string detalle = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Mercado Pago no pudo verificar la renovación automática: {detalle}");
            }

            MercadoPagoPreapprovalSearchResponse? busqueda = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalSearchResponse>();
            MercadoPagoPreapprovalResponse? resultado = busqueda?.Results?.Where(item => string.Equals(item.ExternalReference, referenciaExterna, StringComparison.Ordinal)).OrderByDescending(item => item.LastModified).FirstOrDefault();
            if (resultado is null || string.IsNullOrWhiteSpace(resultado.Id) || string.IsNullOrWhiteSpace(resultado.Status))
                throw new InvalidOperationException("Mercado Pago no encontró la autorización de renovación solicitada.");
            return CrearPreapproval(resultado, referenciaExterna);
        }

        public async Task<List<MercadoPagoAuthorizedPayment>> ConsultarCobrosRecurrentes(string preapprovalId)
        {
            string? accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/authorized_payments/search?preapproval_id={Uri.EscapeDataString(preapprovalId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("No fue posible consultar los cobros recurrentes en Mercado Pago.");
            MercadoPagoAuthorizedPaymentsResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoAuthorizedPaymentsResponse>();
            return resultado?.Results?.Where(x => x.Payment?.Status?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true).Select(x => new MercadoPagoAuthorizedPayment(x.Id.ToString(), x.TransactionAmount, x.CurrencyId ?? "ARS", x.Payment!.Id.ToString())).ToList() ?? new List<MercadoPagoAuthorizedPayment>();
        }

        public async Task<MercadoPagoPreapproval> ActualizarRenovacionAutomatica(string preapprovalId, string estado)
        {
            if (string.IsNullOrWhiteSpace(preapprovalId))
                throw new ArgumentException("No existe una autorización recurrente para actualizar.");
            if (estado is not ("authorized" or "paused"))
                throw new ArgumentException("El estado solicitado para la autorización recurrente no es válido.");
            string? accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://api.mercadopago.com/preapproval/{Uri.EscapeDataString(preapprovalId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            SolicitudEstadoMercadoPago solicitud = new SolicitudEstadoMercadoPago(estado);
            request.Content = JsonContent.Create(solicitud);
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Mercado Pago no pudo actualizar la renovación automática: {await response.Content.ReadAsStringAsync()}");
            MercadoPagoPreapprovalResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPreapprovalResponse>();
            if (resultado is null || string.IsNullOrWhiteSpace(resultado.Id) || string.IsNullOrWhiteSpace(resultado.Status))
                throw new InvalidOperationException("Mercado Pago no devolvió el estado de la autorización recurrente.");
            return CrearPreapproval(resultado, string.Empty);
        }

        private static MercadoPagoPreapproval CrearPreapproval(MercadoPagoPreapprovalResponse resultado, string referenciaAlternativa)
        {
            return new MercadoPagoPreapproval(resultado.Id!, resultado.Status!, resultado.InitPoint ?? string.Empty, resultado.ExternalReference ?? referenciaAlternativa)
            {
                NextPaymentDate = resultado.NextPaymentDate,
                StartDate = resultado.AutoRecurring?.StartDate,
            };
        }

        private (decimal Importe, string Moneda) ObtenerCobroPrueba(string? codigoPlan)
        {
            string? importeConfigurado = _configuration[$"MercadoPago:CobroPrueba:ImportesPorPlan:{codigoPlan}"] ?? _configuration["MercadoPago:CobroPrueba:ImportePredeterminado"];
            decimal importe = decimal.TryParse(importeConfigurado, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importeLeido) ? importeLeido : 1000m;
            string moneda = _configuration["MercadoPago:CobroPrueba:Moneda"] ?? "ARS";
            return (importe, moneda);
        }

        private bool CobroPruebaHabilitado() => bool.TryParse(_configuration["MercadoPago:CobroPrueba:Habilitado"], out bool habilitado) && habilitado;
        public async Task<long?> BuscarPagoActivo(PagoSuscripcion parte)
        {
            string referencia = Uri.EscapeDataString(parte.ReferenciaInterna);
            using var solicitud = CrearSolicitudAutenticada(HttpMethod.Get,
                $"https://api.mercadopago.com/v1/payments/search?external_reference={referencia}&sort=date_created&criteria=desc");
            using HttpResponseMessage respuesta = await _httpClient.SendAsync(solicitud);
            respuesta.EnsureSuccessStatusCode();
            var busqueda = await respuesta.Content.ReadFromJsonAsync<BusquedaPagos>()
                ?? throw new InvalidOperationException("Mercado Pago no devolvió el estado del pago pendiente.");

            return busqueda.Results
                .Where(pago => pago.Id > 0 && pago.ExternalReference == parte.ReferenciaInterna &&
                    pago.TransactionAmount == parte.Importe && string.Equals(pago.CurrencyId, parte.Moneda, StringComparison.OrdinalIgnoreCase) &&
                    pago.Status is "approved" or "pending" or "in_process" or "authorized" or "in_mediation")
                .OrderByDescending(pago => pago.Status == "approved")
                .Select(pago => (long?)pago.Id)
                .FirstOrDefault();
        }

        public async Task HabilitarOtrosMedios(string urlPago)
        {
            if (!Uri.TryCreate(urlPago, UriKind.Absolute, out Uri? url))
                throw new InvalidOperationException("Mercado Pago devolvió un enlace de pago inválido.");
            string? identificador = url.Query.TrimStart('?').Split('&')
                .FirstOrDefault(parametro => parametro.StartsWith("pref_id=", StringComparison.Ordinal))?["pref_id=".Length..];
            if (string.IsNullOrWhiteSpace(identificador))
                throw new InvalidOperationException("No encontramos la preferencia de Mercado Pago para habilitar otros medios.");

            using var solicitud = CrearSolicitudAutenticada(HttpMethod.Put,
                $"https://api.mercadopago.com/checkout/preferences/{Uri.EscapeDataString(Uri.UnescapeDataString(identificador))}");
            solicitud.Content = JsonContent.Create(new
            {
                payment_methods = new { excluded_payment_types = Array.Empty<object>(), excluded_payment_methods = Array.Empty<object>() },
            });
            using HttpResponseMessage respuesta = await _httpClient.SendAsync(solicitud);
            respuesta.EnsureSuccessStatusCode();
        }

        private sealed class BusquedaPagos
        {
            public required List<MercadoPagoPaymentResponse> Results { get; init; }
        }

        private HttpRequestMessage CrearSolicitudAutenticada(HttpMethod metodo, string url)
        {
            string? token = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("No se configuró el Access Token de Mercado Pago.");
            var solicitud = new HttpRequestMessage(metodo, url);
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return solicitud;
        }

        public async Task<MercadoPagoPayment> ConsultarPago(string paymentId)
        {
            if (!long.TryParse(paymentId, out long id) || id <= 0)
                throw new ArgumentException("Mercado Pago no informó un identificador de pago válido.");
            using var request = CrearSolicitudAutenticada(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{id}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"No fue posible verificar el pago en Mercado Pago: {error}");
            }

            MercadoPagoPaymentResponse? resultado = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>();
            if (resultado is null || resultado.Id <= 0 || string.IsNullOrWhiteSpace(resultado.Status) || string.IsNullOrWhiteSpace(resultado.ExternalReference) || string.IsNullOrWhiteSpace(resultado.CurrencyId))
            {
                throw new InvalidOperationException("Mercado Pago no devolvió los datos necesarios para verificar el pago.");
            }

            MercadoPagoPayment pago = new MercadoPagoPayment(resultado.Id.ToString(), resultado.Status, resultado.ExternalReference, resultado.TransactionAmount, resultado.CurrencyId);
            return pago;
        }
    }
}
