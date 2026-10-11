using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services;

public sealed class StripeService
{
    private const string Proveedor = "Stripe";
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public StripeService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> CrearPago(ContratacionPendiente contratacion, PlanComercial plan)
    {
        return await CrearCheckoutPago("Suscripcion TeamBalance", contratacion.Importe, contratacion.Moneda, contratacion.ReferenciaContratacion, plan.Nombre);
    }

    public async Task<string> CrearPago(OperacionSuscripcionPendiente operacion, PlanComercial plan)
    {
        return await CrearCheckoutPago($"TeamBalance - cambio a {plan.Nombre}", operacion.Importe, operacion.Moneda, operacion.ReferenciaInterna, plan.Nombre);
    }

    public async Task<string> CrearRenovacionAutomatica(Suscripcion suscripcion, PlanComercial plan)
    {
        string referencia = $"ren-{suscripcion.ID}";
        DateTimeOffset fechaPrueba = new DateTimeOffset(suscripcion.FechaVencimiento);
        long trialEnd = fechaPrueba.ToUnixTimeSeconds();
        if (trialEnd <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            trialEnd = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        }

        Dictionary<string, string> datos = CrearDatosRetorno(referencia);
        datos["mode"] = "subscription";
        datos["client_reference_id"] = referencia;
        datos["metadata[tipo]"] = "RenovacionAutomatica";
        datos["metadata[idSuscripcion]"] = suscripcion.ID.ToString(CultureInfo.InvariantCulture);
        datos["subscription_data[metadata][tipo]"] = "RenovacionAutomatica";
        datos["subscription_data[metadata][idSuscripcion]"] = suscripcion.ID.ToString(CultureInfo.InvariantCulture);
        datos["subscription_data[trial_end]"] = trialEnd.ToString(CultureInfo.InvariantCulture);
        datos["line_items[0][price_data][currency]"] = NormalizarMoneda(plan.Moneda);
        datos["line_items[0][price_data][product_data][name]"] = $"TeamBalance {plan.Nombre}";
        datos["line_items[0][price_data][unit_amount]"] = ConvertirAUnidadMenor(plan.PrecioVigente, plan.Moneda).ToString(CultureInfo.InvariantCulture);
        datos["line_items[0][price_data][recurring][interval]"] = "month";
        datos["line_items[0][price_data][recurring][interval_count]"] = Math.Max(plan.DuracionMeses, 1).ToString(CultureInfo.InvariantCulture);
        datos["line_items[0][quantity]"] = "1";

        return await CrearSesion(datos);
    }

    public async Task<(string Id, string Estado, string Referencia, decimal Importe, string Moneda, string ReferenciaRenovacion)> ConsultarPago(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Stripe no informo la sesion de pago.", nameof(sessionId));

        using HttpResponseMessage respuesta = await Enviar(HttpMethod.Get, $"https://api.stripe.com/v1/checkout/sessions/{Uri.EscapeDataString(sessionId)}", null);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"Stripe no pudo consultar la sesion de pago: {contenido}");

        using JsonDocument documento = JsonDocument.Parse(contenido);
        JsonElement sesion = documento.RootElement;
        string id = ObtenerTexto(sesion, "id");
        string estadoSesion = ObtenerTexto(sesion, "status");
        string estadoPago = ObtenerTexto(sesion, "payment_status");
        string referencia = ObtenerTexto(sesion, "client_reference_id");
        if (string.IsNullOrWhiteSpace(referencia) && sesion.TryGetProperty("metadata", out JsonElement metadata))
        {
            referencia = ObtenerTexto(metadata, "referencia");
        }

        long totalMenor = sesion.TryGetProperty("amount_total", out JsonElement total) && total.ValueKind != JsonValueKind.Null ? total.GetInt64() : 0L;
        string moneda = ObtenerTexto(sesion, "currency").ToUpperInvariant();
        string referenciaRenovacion = ObtenerTexto(sesion, "subscription");
        string estado = estadoPago.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                        (estadoSesion.Equals("complete", StringComparison.OrdinalIgnoreCase) && estadoPago.Equals("no_payment_required", StringComparison.OrdinalIgnoreCase))
            ? "approved"
            : estadoSesion.Equals("expired", StringComparison.OrdinalIgnoreCase) ? "cancelled" : "pending";

        return (id, estado, referencia, ConvertirDesdeUnidadMenor(totalMenor, moneda), moneda, referenciaRenovacion);
    }

    public async Task<string> ActualizarRenovacionAutomatica(string referenciaRenovacion, bool activa)
    {
        if (string.IsNullOrWhiteSpace(referenciaRenovacion)) throw new ArgumentException("No existe una autorizacion recurrente de Stripe.", nameof(referenciaRenovacion));

        Dictionary<string, string> datos = new Dictionary<string, string>
        {
            ["cancel_at_period_end"] = activa ? "false" : "true",
        };
        using HttpResponseMessage respuesta = await Enviar(HttpMethod.Post, $"https://api.stripe.com/v1/subscriptions/{Uri.EscapeDataString(referenciaRenovacion)}", datos);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"Stripe no pudo actualizar la renovacion automatica: {contenido}");
        using JsonDocument documento = JsonDocument.Parse(contenido);
        return ObtenerTexto(documento.RootElement, "status");
    }

    public async Task<List<(string ReferenciaPago, decimal Importe, string Moneda, string Estado)>> ConsultarCobrosRecurrentes(string referenciaRenovacion)
    {
        using HttpResponseMessage respuesta = await Enviar(HttpMethod.Get, $"https://api.stripe.com/v1/invoices?subscription={Uri.EscapeDataString(referenciaRenovacion)}&status=paid&limit=100", null);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"Stripe no pudo consultar los cobros recurrentes: {contenido}");

        using JsonDocument documento = JsonDocument.Parse(contenido);
        List<(string ReferenciaPago, decimal Importe, string Moneda, string Estado)> pagos = new List<(string ReferenciaPago, decimal Importe, string Moneda, string Estado)>();
        if (!documento.RootElement.TryGetProperty("data", out JsonElement facturas) || facturas.ValueKind != JsonValueKind.Array) return pagos;

        foreach (JsonElement factura in facturas.EnumerateArray())
        {
            long importeMenor = factura.TryGetProperty("amount_paid", out JsonElement importe) ? importe.GetInt64() : 0L;
            if (importeMenor <= 0) continue;
            string moneda = ObtenerTexto(factura, "currency").ToUpperInvariant();
            pagos.Add((ObtenerTexto(factura, "id"), ConvertirDesdeUnidadMenor(importeMenor, moneda), moneda, ObtenerTexto(factura, "status")));
        }

        return pagos;
    }

    public bool ValidarFirmaWebhook(string contenido, string? encabezadoFirma)
    {
        string? secreto = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secreto) || string.IsNullOrWhiteSpace(encabezadoFirma)) return false;

        string? marcaTiempo = null;
        string? firma = null;
        foreach (string parte in encabezadoFirma.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] claveValor = parte.Split('=', 2);
            if (claveValor.Length != 2) continue;
            if (claveValor[0] == "t") marcaTiempo = claveValor[1];
            if (claveValor[0] == "v1") firma = claveValor[1];
        }
        if (string.IsNullOrWhiteSpace(marcaTiempo) || string.IsNullOrWhiteSpace(firma)) return false;

        using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secreto));
        byte[] firmaCalculada = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{marcaTiempo}.{contenido}"));
        byte[] firmaRecibida;
        try { firmaRecibida = Convert.FromHexString(firma); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(firmaCalculada, firmaRecibida);
    }

    private async Task<string> CrearCheckoutPago(string titulo, decimal importe, string moneda, string referencia, string? codigoPlan)
    {
        Dictionary<string, string> datos = CrearDatosRetorno(referencia);
        datos["mode"] = "payment";
        datos["client_reference_id"] = referencia;
        datos["metadata[tipo]"] = referencia.StartsWith("sus-", StringComparison.Ordinal) ? "ActualizacionSuscripcion" : "Contratacion";
        datos["metadata[referencia]"] = referencia;
        datos["payment_intent_data[metadata][referencia]"] = referencia;
        datos["line_items[0][price_data][currency]"] = NormalizarMoneda(moneda);
        datos["line_items[0][price_data][product_data][name]"] = titulo;
        datos["line_items[0][price_data][product_data][metadata][plan]"] = codigoPlan ?? string.Empty;
        datos["line_items[0][price_data][unit_amount]"] = ConvertirAUnidadMenor(importe, moneda).ToString(CultureInfo.InvariantCulture);
        datos["line_items[0][quantity]"] = "1";

        return await CrearSesion(datos);
    }

    private async Task<string> CrearSesion(Dictionary<string, string> datos)
    {
        using HttpResponseMessage respuesta = await Enviar(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions", datos);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"Stripe rechazo la solicitud de pago: {contenido}");
        using JsonDocument documento = JsonDocument.Parse(contenido);
        string url = ObtenerTexto(documento.RootElement, "url");
        return string.IsNullOrWhiteSpace(url) ? throw new InvalidOperationException("Stripe no devolvio una URL de pago.") : url;
    }

    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, Dictionary<string, string>? datos)
    {
        string claveSecreta = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("No se configuro Stripe:SecretKey. Agregala a los secretos locales de la API.");
        HttpRequestMessage solicitud = new HttpRequestMessage(metodo, url);
        string credencial = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{claveSecreta}:"));
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Basic", credencial);
        if (datos is not null) solicitud.Content = new FormUrlEncodedContent(datos);
        HttpResponseMessage respuesta = await _httpClient.SendAsync(solicitud);
        solicitud.Dispose();
        return respuesta;
    }

    private Dictionary<string, string> CrearDatosRetorno(string referencia)
    {
        string urlPublica = _configuration["Frontend:PublicBaseUrl"] ?? throw new InvalidOperationException("Configura Frontend:PublicBaseUrl con la URL HTTPS publica del frontend para usar Stripe.");
        if (!Uri.TryCreate(urlPublica, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Frontend:PublicBaseUrl debe ser una URL publica con HTTPS para usar Stripe.");
        string retorno = urlPublica.TrimEnd('/') + "/pago/resultado?external_reference=" + Uri.EscapeDataString(referencia);
        return new Dictionary<string, string>
        {
            ["success_url"] = retorno + "&session_id={CHECKOUT_SESSION_ID}",
            ["cancel_url"] = retorno + "&cancelled=true",
        };
    }

    private static string NormalizarMoneda(string moneda) => moneda.Trim().ToLowerInvariant();

    private static long ConvertirAUnidadMenor(decimal importe, string moneda)
    {
        if (!moneda.Equals("USD", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La integracion actual de Stripe admite cobros en USD.");
        return decimal.ToInt64(decimal.Round(importe * 100m, 0, MidpointRounding.AwayFromZero));
    }

    private static decimal ConvertirDesdeUnidadMenor(long importe, string moneda)
    {
        if (!moneda.Equals("USD", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Stripe devolvio una moneda no compatible con la configuracion comercial.");
        return importe / 100m;
    }

    private static string ObtenerTexto(JsonElement elemento, string propiedad)
    {
        return elemento.TryGetProperty(propiedad, out JsonElement valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() ?? string.Empty : string.Empty;
    }
}
