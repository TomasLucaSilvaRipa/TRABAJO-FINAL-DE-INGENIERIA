using TeamBalance.BLL;

namespace Teambalance.API.Services;

public sealed class OperationalEmailHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationalEmailHostedService> _logger;
    public OperationalEmailHostedService(IServiceScopeFactory scopeFactory, ILogger<OperationalEmailHostedService> logger) { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                int enviados = await scope.ServiceProvider.GetRequiredService<BLLCorreoOperativo>().ProcesarPendientes();
                int alertas = await scope.ServiceProvider.GetRequiredService<BLLAlertaOperativa>().Procesar();
                if (enviados > 0) _logger.LogInformation("Se enviaron {Cantidad} correos operativos.", enviados);
                if (alertas > 0) _logger.LogInformation("Se generaron {Cantidad} alertas operativas nuevas.", alertas);
            }
            catch (Exception ex) { _logger.LogError(ex, "No se pudo procesar la cola de correos operativos."); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
