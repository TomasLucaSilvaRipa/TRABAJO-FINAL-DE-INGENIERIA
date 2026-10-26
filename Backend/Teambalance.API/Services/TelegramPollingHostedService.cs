using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Services;
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
        if (!_telegram.Configurado)
        {
            _logger.LogInformation("Telegram no se inicia porque Telegram:BotToken no está configurado.");
            return;
        }

        long offset = 0;
        try
        {
            await _telegram.RegistrarEstadoWebhookAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible consultar el estado del webhook de Telegram al iniciar.");
        }

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
            catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
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
        if (mensaje?.Chat is null || mensaje.From is null || !string.Equals(mensaje.Chat.Type, "private", StringComparison.OrdinalIgnoreCase))
            return;
        if (string.IsNullOrWhiteSpace(mensaje.Text) || !mensaje.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            return;
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
            await _telegram.EnviarMensajeAsync(mensaje.Chat.Id, "No pudimos usar este enlace. Puede haber vencido o haber sido reemplazado por una solicitud más reciente. Volvé a TeamBalance, generá un enlace nuevo y usá únicamente el último comando.", cancellationToken);
            return;
        }

        bool enviado = await _telegram.EnviarCodigoAsync(entrega.ChatId, entrega.Codigo, string.Equals(entrega.Tipo, "Registro", StringComparison.Ordinal) ? "Validación de cuenta" : "Vinculación de Telegram", cancellationToken);
        if (!enviado)
            _logger.LogError("Telegram no aceptó el código después de tres intentos para el chat {ChatId}. El usuario podrá solicitar un reenvío desde TeamBalance.", entrega.ChatId);
    }
}
