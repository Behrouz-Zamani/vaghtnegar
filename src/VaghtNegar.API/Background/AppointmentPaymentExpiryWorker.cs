using Microsoft.EntityFrameworkCore;
using VaghtNegar.API.Endpoints;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.API.Background;

public sealed class AppointmentPaymentExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<AppointmentPaymentExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<VaghtNegarDbContext>();
                await AppointmentPaymentEndpoints.ExpireDueAsync(db, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Failed to expire pending appointment payments."); }
            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
