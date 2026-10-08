using TeamBalance.BLL;

namespace Teambalance.API.Services;

public sealed class BackupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupHostedService> _logger;

    public BackupHostedService(IServiceScopeFactory scopeFactory, ILogger<BackupHostedService> logger) { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BLLRespaldoBaseDatos>().EjecutarRespaldoDiarioSiCorresponde();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo ejecutar el respaldo diario de TeamBalance.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
