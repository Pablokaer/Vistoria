using InspectFlow.Modules.Inspections.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InspectFlow.Infrastructure.Background;

/// <summary>Periodically moves Open inspections past their acceptance deadline to Expired.</summary>
public sealed class InspectionExpiryWorker(IServiceScopeFactory scopes, ILogger<InspectionExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var expired = await scope.ServiceProvider.GetRequiredService<InspectionMaintenanceService>().ExpireOverdueAsync(stoppingToken);
                if (expired > 0) logger.LogInformation("Expired {Count} overdue inspections", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Inspection expiry run failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
