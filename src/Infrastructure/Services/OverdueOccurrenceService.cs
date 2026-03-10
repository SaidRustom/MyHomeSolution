using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OverdueOccurrenceService(
    IServiceScopeFactory scopeFactory,
    ITaskProcessingLock processingLock,
    IOptions<OverdueOccurrenceOptions> options,
    ILogger<OverdueOccurrenceService> logger)
    : MonitoredBackgroundService<OverdueOccurrenceService>(scopeFactory, logger),
      IMonitoredBackgroundService
{
    private readonly OverdueOccurrenceOptions _options = options.Value;
    private const int MaxRetries = 3;

    public static Guid ServiceId => BackgroundServiceSeeder.ServiceIds.OverdueOccurrence;
    public static string ServiceName => "Overdue Occurrence Checker";
    public static string ServiceDescription =>
        "Scans for pending task occurrences that have passed their due date and marks them as overdue.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Overdue occurrence service started — interval {Interval}m",
            _options.IntervalMinutes);

        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        do
        {
            try
            {
                await RunMonitoredCycleAsync(MarkOverdueOccurrencesAsync, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during overdue occurrence check");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Overdue occurrence service stopped");
    }

    private async Task<string?> MarkOverdueOccurrencesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var today = dateTimeProvider.Today;

        var overdueOccurrences = await dbContext.TaskOccurrences
            .Include(o => o.HouseholdTask)
            .Where(o => !o.IsDeleted
                && o.DueDate < today
                && o.Status == OccurrenceStatus.Pending
                && o.HouseholdTask.IsActive
                && !o.HouseholdTask.IsDeleted)
            .ToListAsync(cancellationToken);

        if (overdueOccurrences.Count == 0)
            return "No overdue occurrences found";

        var grouped = overdueOccurrences.GroupBy(o => o.HouseholdTaskId);
        var markedCount = 0;

        foreach (var group in grouped)
        {
            var taskId = group.Key;

            try
            {
                await using var lockHandle = await processingLock.TryAcquireAsync(
                    taskId, TimeSpan.FromSeconds(5), cancellationToken);

                if (lockHandle is null)
                {
                    logger.LogDebug("Skipping overdue check for task {TaskId} — lock held", taskId);
                    continue;
                }

                for (var attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        await using var taskScope = scopeFactory.CreateAsyncScope();
                        var taskDbContext = taskScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                        var taskDateTimeProvider = taskScope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                        var taskToday = taskDateTimeProvider.Today;

                        var freshOccurrences = await taskDbContext.TaskOccurrences
                            .Include(o => o.HouseholdTask)
                            .Where(o => !o.IsDeleted
                                && o.HouseholdTaskId == taskId
                                && o.DueDate < taskToday
                                && o.Status == OccurrenceStatus.Pending
                                && o.HouseholdTask.IsActive
                                && !o.HouseholdTask.IsDeleted)
                            .ToListAsync(cancellationToken);

                        if (freshOccurrences.Count == 0)
                            break;

                        foreach (var occurrence in freshOccurrences)
                        {
                            occurrence.Status = OccurrenceStatus.Overdue;
                        }

                        await taskDbContext.SaveChangesAsync(cancellationToken);
                        markedCount += freshOccurrences.Count;

                        var taskPublisher = taskScope.ServiceProvider.GetRequiredService<IPublisher>();
                        foreach (var occurrence in freshOccurrences)
                        {
                            try
                            {
                                await taskPublisher.Publish(
                                    new OccurrenceOverdueEvent(
                                        occurrence.Id,
                                        occurrence.HouseholdTaskId,
                                        occurrence.HouseholdTask.Title,
                                        occurrence.AssignedToUserId),
                                    cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex,
                                    "Failed to publish overdue notification for occurrence {OccurrenceId}",
                                    occurrence.Id);
                            }
                        }

                        break;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        logger.LogWarning(ex,
                            "Concurrency conflict marking overdue for task {TaskId} (attempt {Attempt}/{MaxRetries})",
                            taskId, attempt, MaxRetries);

                        if (attempt == MaxRetries)
                            throw;

                        await Task.Delay(100 * attempt, cancellationToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to mark overdue occurrences for task {TaskId}", taskId);
            }
        }

        if (markedCount > 0)
        {
            logger.LogInformation("Marked {Count} occurrences as overdue", markedCount);
        }

        return $"Marked {markedCount} occurrences as overdue";
    }
}
