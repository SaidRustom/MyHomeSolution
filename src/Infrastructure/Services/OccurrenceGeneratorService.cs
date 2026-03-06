using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OccurrenceGeneratorService(
    IServiceScopeFactory scopeFactory,
    IOptions<OccurrenceGeneratorOptions> options,
    ILogger<OccurrenceGeneratorService> logger)
    : BackgroundService
{
    private readonly OccurrenceGeneratorOptions _options = options.Value;

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
                await GenerateOccurrencesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during occurrence generation cycle");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Occurrence generator stopped");
    }

    private async Task GenerateOccurrencesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var recurringTasks = await dbContext.HouseholdTasks
            .IgnoreQueryFilters()
            .Where(t => t.IsRecurring && t.IsActive && !t.IsDeleted)
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees.OrderBy(a => a.Order))
            .Include(t => t.Occurrences.Where(o => !o.IsDeleted))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var today = dateTimeProvider.Today;
        var generated = 0;

        foreach (var task in recurringTasks)
        {
            if (task.RecurrencePattern is null)
            {
                logger.LogWarning("Recurring task {TaskId} has no recurrence pattern — skipping", task.Id);
                continue;
            }

            try
            {
                generated += GenerateOccurrencesForTask(task, today);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate occurrences for task {TaskId}", task.Id);
            }
        }

        if (generated > 0)
        {
            var saved = await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Generated {Count} occurrences across {Tasks} tasks ({Rows} rows saved)",
                generated, recurringTasks.Count, saved);
        }
    }

    private int GenerateOccurrencesForTask(HouseholdTask task, DateOnly today)
    {
        var pattern = task.RecurrencePattern!;

        var pendingFutureCount = task.Occurrences
            .Count(o => o.DueDate >= today
                        && o.Status is OccurrenceStatus.Pending or OccurrenceStatus.InProgress);

        if (pendingFutureCount >= _options.RequiredFutureOccurrences)
            return 0;

        var toGenerate = _options.RequiredFutureOccurrences - pendingFutureCount;

        var lastOccurrenceDate = task.Occurrences
            .Select(o => (DateOnly?)o.DueDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        var nextDate = lastOccurrenceDate.HasValue
            ? pattern.GetNextOccurrenceDate(lastOccurrenceDate.Value)
            : MaxDate(pattern.StartDate, today);

        var existingDueDates = task.Occurrences.Select(o => o.DueDate).ToHashSet();
        var created = 0;

        for (var i = 0; i < toGenerate; i++)
        {
            if (pattern.EndDate.HasValue && nextDate > pattern.EndDate.Value)
                break;

            if (!existingDueDates.Add(nextDate))
            {
                nextDate = pattern.GetNextOccurrenceDate(nextDate);
                continue;
            }

            var assigneeUserId = pattern.GetNextAssigneeUserId();
            pattern.AdvanceAssigneeIndex();

            task.Occurrences.Add(new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = nextDate,
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = assigneeUserId
            });

            nextDate = pattern.GetNextOccurrenceDate(nextDate);
            created++;
        }

        return created;
    }

    private static DateOnly MaxDate(DateOnly a, DateOnly b) => a >= b ? a : b;
}
