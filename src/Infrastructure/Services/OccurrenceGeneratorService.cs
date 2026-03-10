using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OccurrenceGeneratorService(
    IServiceScopeFactory scopeFactory,
    ITaskProcessingLock processingLock,
    IOptions<OccurrenceGeneratorOptions> options,
    ILogger<OccurrenceGeneratorService> logger)
    : MonitoredBackgroundService<OccurrenceGeneratorService>(scopeFactory, logger),
      IMonitoredBackgroundService
{
    private readonly OccurrenceGeneratorOptions _options = options.Value;
    private const int MaxRetries = 3;

    public static Guid ServiceId => BackgroundServiceSeeder.ServiceIds.OccurrenceGenerator;
    public static string ServiceName => "Occurrence Generator";
    public static string ServiceDescription =>
        "Generates future task occurrences for recurring household tasks based on their recurrence patterns.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Occurrence generator started — interval {Interval}m, target {Count} future occurrences",
            _options.IntervalMinutes,
            _options.RequiredFutureOccurrences);

        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        // Run immediately on first tick, then on timer
        do
        {
            try
            {
                await RunMonitoredCycleAsync(GenerateOccurrencesAsync, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during occurrence generation cycle");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Occurrence generator stopped");
    }

    private async Task<string?> GenerateOccurrencesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var recurringTaskIds = await dbContext.HouseholdTasks
            .IgnoreQueryFilters()
            .Where(t => t.IsRecurring && t.IsActive && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var today = dateTimeProvider.Today;
        var generated = 0;
        var failed = 0;

        foreach (var taskId in recurringTaskIds)
        {
            try
            {
                await using var lockHandle = await processingLock.TryAcquireAsync(
                    taskId, TimeSpan.FromSeconds(5), cancellationToken);

                if (lockHandle is null)
                {
                    logger.LogDebug("Skipping task {TaskId} — another process holds the lock", taskId);
                    continue;
                }

                var count = await TopUpWithRetryAsync(taskId, today, cancellationToken);
                generated += count;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to generate occurrences for task {TaskId}", taskId);
                failed++;
            }
        }

        if (generated > 0 || failed > 0)
        {
            logger.LogInformation(
                "Occurrence generation cycle complete: {Generated} created, {Failed} tasks failed, {Total} tasks processed",
                generated, failed, recurringTaskIds.Count);
        }

        return $"{generated} created, {failed} failed, {recurringTaskIds.Count} tasks processed";
    }

    private async Task<int> TopUpWithRetryAsync(
        Guid taskId, DateOnly today, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await using var taskScope = scopeFactory.CreateAsyncScope();
                var taskDbContext = taskScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var task = await taskDbContext.HouseholdTasks
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == taskId && !t.IsDeleted)
                    .Include(t => t.RecurrencePattern!)
                        .ThenInclude(rp => rp.Assignees.OrderBy(a => a.Order))
                    .Include(t => t.Occurrences.Where(o => !o.IsDeleted))
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(cancellationToken);

                if (task?.RecurrencePattern is null)
                    return 0;

                var count = OccurrenceReconciler.TopUp(
                    task, today, taskDbContext, _options.RequiredFutureOccurrences);

                if (count > 0)
                {
                    await taskDbContext.SaveChangesAsync(cancellationToken);
                }

                return count;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex,
                    "Concurrency conflict generating occurrences for task {TaskId} (attempt {Attempt}/{MaxRetries})",
                    taskId, attempt, MaxRetries);

                if (attempt == MaxRetries)
                    throw;

                await Task.Delay(100 * attempt, cancellationToken);
            }
        }

        return 0;
    }
}
