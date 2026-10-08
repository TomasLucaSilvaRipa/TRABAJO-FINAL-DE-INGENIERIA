using TeamBalance.BLL;

namespace Teambalance.API.Services;

public sealed class SubscriptionExpirationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpirationHostedService> _logger;

    public SubscriptionExpirationHostedService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpirationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                BLLSuscripcion suscripciones = scope.ServiceProvider.GetRequiredService<BLLSuscripcion>();
                int cobros = 0;
                try
                {
                    cobros = await suscripciones.SincronizarCobrosRecurrentes();
                }
                catch (Exception ex)
                {
                    // Un proveedor temporalmente no disponible no debe impedir actualizar vencimientos locales.
                    _logger.LogError(ex, "No se pudieron conciliar los cobros recurrentes de Mercado Pago.");
                }
                int procesadas = suscripciones.SincronizarVencimientos();
                if (cobros > 0) _logger.LogInformation("Se aplicaron {Cantidad} cobros recurrentes de suscripciones.", cobros);
                if (procesadas > 0) _logger.LogInformation("Se sincronizaron {Cantidad} suscripciones vencidas.", procesadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron sincronizar los vencimientos de suscripciones.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
