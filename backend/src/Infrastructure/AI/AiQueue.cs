using System.Threading.Channels;
using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.AI.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.AI;

/// <summary>In-process queue. Durable state lives in ai_analyses (Pending rows are re-queued on start-up).</summary>
public sealed class ChannelAiAnalysisQueue : IAiAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = false });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid analysisId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(analysisId, cancellationToken);
}

/// <summary>Processes the analysis immediately (tests, or AI__PROCESSINLINE=true).</summary>
public sealed class InlineAiAnalysisQueue(IServiceScopeFactory scopes) : IAiAnalysisQueue
{
    public async ValueTask EnqueueAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AiAnalysisProcessor>().ProcessAsync(analysisId, cancellationToken);
    }
}

public sealed class AiAnalysisWorker(
    ChannelAiAnalysisQueue queue,
    IServiceScopeFactory scopes,
    IOptions<AiOptions> options,
    ILogger<AiAnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingAsync(stoppingToken);
        var workers = Enumerable.Range(0, Math.Max(1, options.Value.MaxConcurrency)).Select(_ => RunAsync(stoppingToken));
        await Task.WhenAll(workers);
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<AiAnalysisProcessor>().ProcessAsync(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing AI analysis {AnalysisId}", id);
            }
        }
    }

    private async Task RequeuePendingAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stale = DateTimeOffset.UtcNow.AddMinutes(-10);
            await db.AiAnalyses.Where(a => a.Status == AiAnalysisStatus.Processing && a.StartedAt < stale && a.Attempts < 3)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AiAnalysisStatus.Pending), ct);
            var pending = await db.AiAnalyses.Where(a => a.Status == AiAnalysisStatus.Pending).Select(a => a.Id).ToListAsync(ct);
            foreach (var id in pending) await queue.EnqueueAsync(id, ct);
            if (pending.Count > 0) logger.LogInformation("Re-queued {Count} pending AI analyses", pending.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not re-queue pending AI analyses (database not ready?)");
        }
    }
}
