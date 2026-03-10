using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Base class for background services that records each execution cycle
/// as a <see cref="BackgroundServiceLog"/> entry. Subclasses implement
/// <see cref="ExecuteCycleAsync"/> for their business logic.
/// </summary>
public abstract class MonitoredBackgroundService<TSelf>(
    IServiceScopeFactory scopeFactory,
    ILogger logger)
    : BackgroundService
    where TSelf : IMonitoredBackgroundService
{
    protected IServiceScopeFactory ScopeFactory { get; } = scopeFactory;

    /// <summary>
    /// Records a background service log entry around <paramref name="cycleAction"/>.
    /// </summary>
    protected async Task RunMonitoredCycleAsync(
        Func<CancellationToken, Task<string?>> cycleAction,
        CancellationToken cancellationToken)
    {
        var log = new BackgroundServiceLog
        {
            BackgroundServiceId = TSelf.ServiceId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = BackgroundServiceRunStatus.Running
        };

        await using var logScope = ScopeFactory.CreateAsyncScope();
        var logDbContext = logScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        logDbContext.BackgroundServiceLogs.Add(log);
        await logDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var resultMessage = await cycleAction(cancellationToken);

            log.Status = BackgroundServiceRunStatus.Completed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ResultMessage = resultMessage;

            await logDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Monitored cycle failed for {ServiceName}", TSelf.ServiceName);

            try
            {
                // Persist exception log and link it
                await using var exScope = ScopeFactory.CreateAsyncScope();
                var exceptionLogService = exScope.ServiceProvider
                    .GetRequiredService<IExceptionLogService>();

                var exceptionLogId = await exceptionLogService.LogAndReturnIdAsync(
                    ex,
                    TSelf.ServiceName,
                    cancellationToken: CancellationToken.None);

                log.ExceptionLogId = exceptionLogId;
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx,
                    "Failed to persist exception log for {ServiceName}", TSelf.ServiceName);
            }

            log.Status = BackgroundServiceRunStatus.Failed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ResultMessage = $"{ex.GetType().Name}: {ex.Message}";

            try
            {
                await logDbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                logger.LogWarning(saveEx,
                    "Failed to update BackgroundServiceLog for {ServiceName}", TSelf.ServiceName);
            }

            throw;
        }
    }
}
