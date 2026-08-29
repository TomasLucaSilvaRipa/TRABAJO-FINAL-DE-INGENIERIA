using System.Net.Http.Json;

namespace TeamBalance.Services;

public class RecaptchaService
{
    private const decimal PuntajeMinimo = 0.5m;
    private readonly HttpClient _httpClient;
    private readonly string? _secretKey;
    private readonly string? _urlPublicaFrontend;

    public RecaptchaService(HttpClient httpClient, string? secretKey, string? urlPublicaFrontend)
    {
        _httpClient = httpClient;
        _secretKey = secretKey;
        _urlPublicaFrontend = urlPublicaFrontend;
    }

    public async Task ValidarLogin(string recaptchaToken)
    {
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            throw new UnauthorizedAccessException("No fue posible validar la verificación de seguridad.");
        }

        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            throw new InvalidOperationException("No se configuró la clave secreta de reCAPTCHA.");
        }

        if (!Uri.TryCreate(_urlPublicaFrontend, UriKind.Absolute, out Uri? urlFrontend))
        {
            throw new InvalidOperationException("No se configuró una URL pública válida para validar reCAPTCHA.");
        }

        List<KeyValuePair<string, string>> datos = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("secret", _secretKey),
            new KeyValuePair<string, string>("response", recaptchaToken),
        };

        using FormUrlEncodedContent contenido = new FormUrlEncodedContent(datos);
        using HttpResponseMessage respuestaHttp = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", contenido);

        if (!respuestaHttp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("No fue posible validar reCAPTCHA en este momento.");
        }

        RespuestaRecaptcha? respuesta = await respuestaHttp.Content.ReadFromJsonAsync<RespuestaRecaptcha>();

        if (respuesta is null || !respuesta.Success || !string.Equals(respuesta.Action, "login", StringComparison.Ordinal) || respuesta.Score < PuntajeMinimo || !string.Equals(respuesta.Hostname, urlFrontend.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("No fue posible validar la verificación de seguridad.");
        }
    }

    private class RespuestaRecaptcha
    {
        public bool Success { get; set; }
        public decimal Score { get; set; }
        public string? Action { get; set; }
        public string? Hostname { get; set; }
    }
}
