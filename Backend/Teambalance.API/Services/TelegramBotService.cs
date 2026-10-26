using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Services;
public sealed class TelegramBotService
{
    private readonly HttpClient _httpClient;
    private readonly string? _botToken;
    private readonly ILogger<TelegramBotService> _logger;
    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.telegram.org/");
        _botToken = configuration["Telegram:BotToken"];
        _logger = logger;
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_botToken);

    public async Task<IReadOnlyList<TelegramUpdate>> ObtenerActualizacionesAsync(long offset, CancellationToken cancellationToken)
    {
        if (!Configurado)
            return Array.Empty<TelegramUpdate>();
        TelegramResponse<List<TelegramUpdate>>? respuesta = await _httpClient.GetFromJsonAsync<TelegramResponse<List<TelegramUpdate>>>(CrearUri("getUpdates", $"?offset={offset}&timeout=25&allowed_updates=%5B%22message%22%5D"), cancellationToken);
        if (respuesta is null || !respuesta.Ok || respuesta.Result is null)
        {
            _logger.LogWarning("Telegram rechazó una solicitud de actualizaciones: {Descripcion}", respuesta?.Description ?? "sin respuesta");
            return Array.Empty<TelegramUpdate>();
        }

        return respuesta.Result;
    }

    public async Task<bool> EnviarCodigoAsync(long chatId, string codigo, string proposito, CancellationToken cancellationToken)
    {
        string texto = $"TeamBalance — {proposito}\n\nTu código de verificación es: {codigo}\n\nVence en 5 minutos. Si no solicitaste esta acción, ignorá este mensaje.";
        return await EnviarMensajeAsync(chatId, texto, cancellationToken);
    }

    public async Task<bool> EnviarMensajeAsync(long chatId, string texto, CancellationToken cancellationToken)
    {
        if (!Configurado || chatId <= 0 || string.IsNullOrWhiteSpace(texto))
            return false;
        for (int intento = 1; intento <= 3; intento++)
        {
            SolicitudEnvioTelegram solicitud = new SolicitudEnvioTelegram(chatId, texto);
            using HttpResponseMessage respuesta = await _httpClient.PostAsJsonAsync(CrearUri("sendMessage"), solicitud, cancellationToken);
            if (respuesta.IsSuccessStatusCode)
                return true;
            _logger.LogWarning("Telegram no aceptó el envío de un mensaje. Estado HTTP: {Estado}. Intento {Intento}/3.", (int)respuesta.StatusCode, intento);
            if (intento < 3)
                await Task.Delay(TimeSpan.FromMilliseconds(700 * intento), cancellationToken);
        }

        return false;
    }

    public async Task RegistrarEstadoWebhookAsync(CancellationToken cancellationToken)
    {
        if (!Configurado)
            return;
        TelegramResponse<TelegramWebhookInfo>? respuesta = await _httpClient.GetFromJsonAsync<TelegramResponse<TelegramWebhookInfo>>(CrearUri("getWebhookInfo"), cancellationToken);
        if (respuesta?.Ok == true && !string.IsNullOrWhiteSpace(respuesta.Result?.Url))
            _logger.LogWarning("El bot tiene un webhook configurado. Long polling no recibirá actualizaciones hasta quitarlo manualmente con deleteWebhook.");
    }

    private Uri CrearUri(string metodo, string sufijo = "")
    {
        return new Uri($"https://api.telegram.org/bot{_botToken}/{metodo}{sufijo}");
    }
}
