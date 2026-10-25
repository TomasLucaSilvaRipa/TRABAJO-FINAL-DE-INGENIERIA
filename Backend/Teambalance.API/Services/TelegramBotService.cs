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
        if (!Configurado) return Array.Empty<TelegramUpdate>();
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
        if (!Configurado || chatId <= 0 || string.IsNullOrWhiteSpace(texto)) return false;
        for (int intento = 1; intento <= 3; intento++)
        {
            SolicitudEnvioTelegram solicitud = new SolicitudEnvioTelegram(chatId, texto);
            using HttpResponseMessage respuesta = await _httpClient.PostAsJsonAsync(CrearUri("sendMessage"), solicitud, cancellationToken);
            if (respuesta.IsSuccessStatusCode) return true;
            _logger.LogWarning("Telegram no aceptó el envío de un mensaje. Estado HTTP: {Estado}. Intento {Intento}/3.", (int)respuesta.StatusCode, intento);
            if (intento < 3) await Task.Delay(TimeSpan.FromMilliseconds(700 * intento), cancellationToken);
        }
        return false;
    }

    public async Task RegistrarEstadoWebhookAsync(CancellationToken cancellationToken)
    {
        if (!Configurado) return;
        TelegramResponse<TelegramWebhookInfo>? respuesta = await _httpClient.GetFromJsonAsync<TelegramResponse<TelegramWebhookInfo>>(CrearUri("getWebhookInfo"), cancellationToken);
        if (respuesta?.Ok == true && !string.IsNullOrWhiteSpace(respuesta.Result?.Url)) _logger.LogWarning("El bot tiene un webhook configurado. Long polling no recibirá actualizaciones hasta quitarlo manualmente con deleteWebhook.");
    }

    private Uri CrearUri(string metodo, string sufijo = "")
    {
        return new Uri($"https://api.telegram.org/bot{_botToken}/{metodo}{sufijo}");
    }
}

public sealed class TelegramPollingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramBotService _telegram;
    private readonly ILogger<TelegramPollingHostedService> _logger;

    public TelegramPollingHostedService(IServiceScopeFactory scopeFactory, TelegramBotService telegram, ILogger<TelegramPollingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_telegram.Configurado) { _logger.LogInformation("Telegram no se inicia porque Telegram:BotToken no está configurado."); return; }
        long offset = 0;
        try { await _telegram.RegistrarEstadoWebhookAsync(stoppingToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "No fue posible consultar el estado del webhook de Telegram al iniciar."); }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<TelegramUpdate> actualizaciones = await _telegram.ObtenerActualizacionesAsync(offset, stoppingToken);
                foreach (TelegramUpdate actualizacion in actualizaciones)
                {
                    await ProcesarActualizacionAsync(actualizacion, stoppingToken);
                    // Confirmamos la actualización recién después de procesarla. Si la base o Telegram
                    // fallan temporalmente, el comando vuelve a intentarse en el próximo ciclo.
                    offset = Math.Max(offset, actualizacion.UpdateId + 1L);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron procesar actualizaciones de Telegram.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcesarActualizacionAsync(TelegramUpdate actualizacion, CancellationToken cancellationToken)
    {
        TelegramMessage? mensaje = actualizacion.Message;
        if (mensaje?.Chat is null || mensaje.From is null || !string.Equals(mensaje.Chat.Type, "private", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrWhiteSpace(mensaje.Text) || !mensaje.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) return;

        string[] partes = mensaje.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length != 2)
        {
            await _telegram.EnviarMensajeAsync(mensaje.Chat.Id, "Abrí nuevamente la vinculación desde TeamBalance y usá el enlace o el comando completo que aparece allí.", cancellationToken);
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        BLLTelegram bllTelegram = scope.ServiceProvider.GetRequiredService<BLLTelegram>();
        TeamBalance.BE.Entidades.EntregaTelegram entrega = bllTelegram.ProcesarInicioBot(partes[1], mensaje.From.Id, mensaje.Chat.Id, mensaje.From.Username);
        if (!entrega.Valida)
        {
            await _telegram.EnviarMensajeAsync(
                mensaje.Chat.Id,
                "No pudimos usar este enlace. Puede haber vencido o haber sido reemplazado por una solicitud más reciente. Volvé a TeamBalance, generá un enlace nuevo y usá únicamente el último comando.",
                cancellationToken);
            return;
        }

        bool enviado = await _telegram.EnviarCodigoAsync(
            entrega.ChatId,
            entrega.Codigo,
            string.Equals(entrega.Tipo, "Registro", StringComparison.Ordinal) ? "Validación de cuenta" : "Vinculación de Telegram",
            cancellationToken);
        if (!enviado) _logger.LogError("Telegram no aceptó el código después de tres intentos para el chat {ChatId}. El usuario podrá solicitar un reenvío desde TeamBalance.", entrega.ChatId);
    }
}

public sealed class TelegramResponse<T>
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("result")] public T? Result { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
}

public sealed class TelegramUpdate
{
    [JsonPropertyName("update_id")] public long UpdateId { get; set; }
    [JsonPropertyName("message")] public TelegramMessage? Message { get; set; }
}

public sealed class TelegramMessage
{
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("chat")] public TelegramChat? Chat { get; set; }
    [JsonPropertyName("from")] public TelegramUser? From { get; set; }
}

public sealed class TelegramChat
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
}

public sealed class TelegramUser
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
}

public sealed class TelegramWebhookInfo
{
    [JsonPropertyName("url")] public string? Url { get; set; }
}
