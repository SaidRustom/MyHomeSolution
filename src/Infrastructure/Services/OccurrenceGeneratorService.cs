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
                generated += GenerateOccurrencesForTask(task, today, dbContext);
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

    internal static int GenerateOccurrencesForTask(
        HouseholdTask task, DateOnly today, IApplicationDbContext dbContext, int requiredFutureOccurrences = 5)
    {
        var pattern = task.RecurrencePattern!;

        var pendingFutureCount = task.Occurrences
            .Count(o => o.DueDate >= today
                        && o.Status is OccurrenceStatus.Pending or OccurrenceStatus.InProgress);

        if (pendingFutureCount >= requiredFutureOccurrences)
            return 0;

        var toGenerate = requiredFutureOccurrences - pendingFutureCount;

        var lastOccurrenceDate = task.Occurrences
            .Select(o => (DateOnly?)o.DueDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        var nextDate = lastOccurrenceDate.HasValue
            ? pattern.GetNextOccurrenceDate(lastOccurrenceDate.Value)
            : MaxDate(pattern.StartDate, today);

        var existingDueDates = task.Occurrences.Select(o => o.DueDate).ToHashSet();
        var created = 0;
        var assignees = pattern.Assignees.OrderBy(a => a.Order).Select(a => a.UserId).ToList();

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

            var occurrence = new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = nextDate,
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = assigneeUserId
            };

            if (task.AutoCreateBill && task.DefaultBillAmount.HasValue && assignees.Count > 0)
            {
                var bill = CreateBillForOccurrence(task, occurrence, assignees);
                dbContext.Bills.Add(bill);
                occurrence.BillId = bill.Id;
            }

            task.Occurrences.Add(occurrence);
            nextDate = pattern.GetNextOccurrenceDate(nextDate);
            created++;
        }

        return created;
    }

    internal static Bill CreateBillForOccurrence(
        HouseholdTask task, TaskOccurrence occurrence, List<string> assigneeUserIds)
    {
        var amount = task.DefaultBillAmount!.Value;
        var currency = task.DefaultBillCurrency ?? "CAD";
        var category = task.DefaultBillCategory ?? BillCategory.General;
        var title = task.DefaultBillTitle ?? $"{task.Title} – {occurrence.DueDate:MMM dd, yyyy}";

        var bill = new Bill
        {
            Title = title,
            Amount = amount,
            Currency = currency,
            Category = category,
            BillDate = new DateTimeOffset(occurrence.DueDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            PaidByUserId = occurrence.AssignedToUserId ?? task.CreatedBy ?? assigneeUserIds[0],
            RelatedEntityId = occurrence.Id,
            RelatedEntityType = "TaskOccurrence"
        };

        var equalPercentage = Math.Round(100m / assigneeUserIds.Count, 2);

        foreach (var userId in assigneeUserIds)
        {
            var splitAmount = Math.Round(amount * equalPercentage / 100m, 2);
            bill.Splits.Add(new BillSplit
            {
                BillId = bill.Id,
                UserId = userId,
                Percentage = equalPercentage,
                Amount = splitAmount,
                Status = SplitStatus.Unpaid
            });
        }

        return bill;
    }

    private int GenerateOccurrencesForTask(HouseholdTask task, DateOnly today, IApplicationDbContext dbContext)
    {
        return GenerateOccurrencesForTask(task, today, dbContext, _options.RequiredFutureOccurrences);
    }

    internal static DateOnly MaxDate(DateOnly a, DateOnly b) => a >= b ? a : b;
}
