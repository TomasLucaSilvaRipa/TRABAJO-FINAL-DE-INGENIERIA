using TeamBalance.BLL;

namespace Teambalance.API.Services;

public sealed class NewsPublisherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsPublisherHostedService> _logger;
    public NewsPublisherHostedService(IServiceScopeFactory scopeFactory, ILogger<NewsPublisherHostedService> logger) { _scopeFactory = scopeFactory; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using IServiceScope scope = _scopeFactory.CreateScope(); int correos = await scope.ServiceProvider.GetRequiredService<BLLNovedades>().DifundirPendientes(); if (correos > 0) _logger.LogInformation("Se enviaron {Cantidad} correos de novedades.", correos); }
            catch (Exception ex) { _logger.LogError(ex, "No se pudo publicar el newsletter pendiente."); }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
