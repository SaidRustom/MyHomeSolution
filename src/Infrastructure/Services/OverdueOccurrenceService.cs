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
    IOptions<OverdueOccurrenceOptions> options,
    ILogger<OverdueOccurrenceService> logger)
    : BackgroundService
{
    private readonly OverdueOccurrenceOptions _options = options.Value;

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
                await MarkOverdueOccurrencesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during overdue occurrence check");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Overdue occurrence service stopped");
    }

    private async Task MarkOverdueOccurrencesAsync(CancellationToken cancellationToken)
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
            return;

        foreach (var occurrence in overdueOccurrences)
        {
            occurrence.Status = OccurrenceStatus.Overdue;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Marked {Count} occurrences as overdue", overdueOccurrences.Count);

        foreach (var occurrence in overdueOccurrences)
        {
            try
            {
                await publisher.Publish(
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
    }
}
