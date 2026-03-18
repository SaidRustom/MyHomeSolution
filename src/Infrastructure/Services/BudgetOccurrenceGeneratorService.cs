using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Background service that checks recurring budgets for expired occurrences and
/// creates new ones. If an occurrence has remaining funds at expiry they are
/// carried over to the new occurrence. A notification is sent to the budget
/// owner when an occurrence expires.
/// </summary>
public sealed class BudgetOccurrenceGeneratorService(
    IServiceScopeFactory scopeFactory,
    IOptions<BudgetOccurrenceGeneratorOptions> options,
    ILogger<BudgetOccurrenceGeneratorService> logger)
    : MonitoredBackgroundService<BudgetOccurrenceGeneratorService>(scopeFactory, logger),
      IMonitoredBackgroundService
{
    private readonly BudgetOccurrenceGeneratorOptions _options = options.Value;

    public static Guid ServiceId => BackgroundServiceSeeder.ServiceIds.BudgetOccurrenceGenerator;
    public static string ServiceName => "Budget Occurrence Generator";
    public static string ServiceDescription =>
        "Creates new budget occurrences when recurring budget periods expire and handles fund carryover.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Budget occurrence generator started — interval {Interval}m",
            _options.IntervalMinutes);

        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        do
        {
            try
            {
                await RunMonitoredCycleAsync(ProcessBudgetOccurrencesAsync, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during budget occurrence generation cycle");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Budget occurrence generator stopped");
    }

    private async Task<string?> ProcessBudgetOccurrencesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = dateTimeProvider.UtcNow;

        // Load all recurring budgets
        var recurringBudgets = await dbContext.Budgets
            .IgnoreQueryFilters()
            .Where(b => b.IsRecurring && !b.IsDeleted)
            .Include(b => b.Occurrences.OrderByDescending(o => o.PeriodEnd))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var created = 0;
        var carryovers = 0;
        var notified = 0;

        foreach (var budget in recurringBudgets)
        {
            try
            {
                var mostRecent = budget.Occurrences.FirstOrDefault();

                // If no occurrences exist yet, create the first one
                if (mostRecent is null)
                {
                    var (periodStart, periodEnd) = CalculatePeriod(budget.StartDate, budget.Period);

                    var newOcc = new BudgetOccurrence
                    {
                        BudgetId = budget.Id,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                        AllocatedAmount = budget.Amount,
                        CarryoverAmount = 0
                    };

                    dbContext.BudgetOccurrences.Add(newOcc);
                    created++;
                    continue;
                }

                // Check if the most recent occurrence has expired
                if (mostRecent.PeriodEnd < now)
                {
                    // Calculate remaining balance
                    var remaining = mostRecent.AllocatedAmount + mostRecent.CarryoverAmount - mostRecent.SpentAmount;

                    // Calculate the next period
                    var nextStart = mostRecent.PeriodEnd;
                    var (periodStart, periodEnd) = CalculateNextPeriod(nextStart, budget.Period);

                    // If budget has an end date and new period is past it, skip
                    if (budget.EndDate.HasValue && periodStart >= budget.EndDate.Value)
                        continue;

                    var carryoverAmount = 0m;
                    string? notes = null;

                    // Transfer remaining funds if non-zero
                    if (remaining != 0)
                    {
                        carryoverAmount = remaining;
                        carryovers++;

                        if (remaining > 0)
                        {
                            notes = $"Carried over ${remaining:N2} surplus from previous period ({mostRecent.PeriodStart:MMM dd} – {mostRecent.PeriodEnd:MMM dd, yyyy}).";
                        }
                        else
                        {
                            notes = $"Carried over ${Math.Abs(remaining):N2} deficit from previous period ({mostRecent.PeriodStart:MMM dd} – {mostRecent.PeriodEnd:MMM dd, yyyy}).";
                        }

                        // Create a transfer record for the carryover
                        var newOcc = new BudgetOccurrence
                        {
                            BudgetId = budget.Id,
                            PeriodStart = periodStart,
                            PeriodEnd = periodEnd,
                            AllocatedAmount = budget.Amount,
                            CarryoverAmount = carryoverAmount,
                            Notes = notes
                        };

                        dbContext.BudgetOccurrences.Add(newOcc);

                        // Record a transfer from old to new occurrence
                        if (remaining > 0)
                        {
                            await mediator.Send(new TransferBudgetFundsCommand
                            {
                                SourceOccurrenceId = mostRecent.Id,
                                DestinationOccurrenceId = newOcc.Id,
                                Amount = remaining,
                                Reason = "Automatic carryover of remaining funds"
                            });
                        }

                        if (budget.ParentBudget != null)
                        {
                            var parent = await dbContext.BudgetOccurrences
                                .IgnoreQueryFilters()
                                .Where(b => b.BudgetId == budget.ParentBudgetId && b.IsActive)
                                .FirstOrDefaultAsync();

                            if(parent == null)
                            {
                                notes += " (Note: Parent budget occurrence not found for transfer)";
                            }
                            else
                            {
                                await mediator.Send(new TransferBudgetFundsCommand
                                {
                                    SourceOccurrenceId = parent.Id,
                                    DestinationOccurrenceId = newOcc.Id,
                                    Amount = budget.Amount,
                                    Reason = "Automatic transfer from parent budget"
                                });
                            }
                        }

                        created++;
                    }
                    else
                    {
                        // No carryover, just create the new occurrence
                        var newOcc = new BudgetOccurrence
                        {
                            BudgetId = budget.Id,
                            PeriodStart = periodStart,
                            PeriodEnd = periodEnd,
                            AllocatedAmount = budget.Amount,
                            CarryoverAmount = 0
                        };

                        if (budget.ParentBudget != null)
                        {
                            var parent = await dbContext.BudgetOccurrences
                                .IgnoreQueryFilters()
                                .Where(b => b.BudgetId == budget.ParentBudgetId && b.IsActive)
                                .FirstOrDefaultAsync();

                            if (parent == null)
                            {
                                notes += " (Note: Parent budget occurrence not found for transfer)";
                            }
                            else
                            {
                                await mediator.Send(new TransferBudgetFundsCommand
                                {
                                    SourceOccurrenceId = parent.Id,
                                    DestinationOccurrenceId = newOcc.Id,
                                    Amount = budget.Amount,
                                    Reason = "Automatic transfer from parent budget"
                                });
                            }
                        }

                        dbContext.BudgetOccurrences.Add(newOcc);
                        created++;
                    }

                    // Notify the budget owner that the occurrence expired
                    if (!string.IsNullOrEmpty(budget.CreatedBy))
                    {
                        var notificationMessage = remaining != 0
                            ? $"Budget '{budget.Name}' period ({mostRecent.PeriodStart:MMM dd} – {mostRecent.PeriodEnd:MMM dd}) has expired. ${Math.Abs(remaining):N2} {(remaining > 0 ? "surplus" : "deficit")} carried over to the new period."
                            : $"Budget '{budget.Name}' period ({mostRecent.PeriodStart:MMM dd} – {mostRecent.PeriodEnd:MMM dd}) has expired. A new period has been created.";

                        var notification = new Notification
                        {
                            Title = "Budget Period Expired",
                            Description = notificationMessage,
                            Type = NotificationType.BudgetPeriodExpired,
                            FromUserId = budget.CreatedBy,
                            ToUserId = budget.CreatedBy,
                            RelatedEntityId = budget.Id,
                            RelatedEntityType = "Budget"
                        };

                        dbContext.Notifications.Add(notification);
                        notified++;

                        try
                        {
                            await realtimeNotificationService.SendUserNotificationAsync(
                                budget.CreatedBy,
                                new Application.Common.Models.UserPushNotification
                                {
                                    EventType = "BudgetPeriodExpired",
                                    NotificationId = notification.Id,
                                    Title = "Budget Period Expired",
                                    Description = notificationMessage,
                                    RelatedEntityType = "Budget",
                                    RelatedEntityId = budget.Id,
                                    OccurredAt = DateTimeOffset.UtcNow
                                },
                                cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to send realtime notification for budget {BudgetId}", budget.Id);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process budget occurrence for budget {BudgetId}", budget.Id);
            }
        }

        if (created > 0 || carryovers > 0 || notified > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Budget occurrence cycle complete: {Created} created, {Carryovers} carryovers, {Notified} notifications, {Total} budgets processed",
                created, carryovers, notified, recurringBudgets.Count);
        }

        return $"{created} created, {carryovers} carryovers, {notified} notifications, {recurringBudgets.Count} budgets processed";
    }

    private static (DateTimeOffset Start, DateTimeOffset End) CalculatePeriod(
        DateTimeOffset startDate, BudgetPeriod period)
    {
        return period switch
        {
            BudgetPeriod.Weekly => (startDate, startDate.AddDays(7)),
            BudgetPeriod.Monthly => (startDate, startDate.AddMonths(1)),
            BudgetPeriod.Annually => (startDate, startDate.AddYears(1)),
            _ => (startDate, startDate.AddMonths(1))
        };
    }

    private static (DateTimeOffset Start, DateTimeOffset End) CalculateNextPeriod(
        DateTimeOffset previousEnd, BudgetPeriod period)
    {
        return period switch
        {
            BudgetPeriod.Weekly => (previousEnd, previousEnd.AddDays(7)),
            BudgetPeriod.Monthly => (previousEnd, previousEnd.AddMonths(1)),
            BudgetPeriod.Annually => (previousEnd, previousEnd.AddYears(1)),
            _ => (previousEnd, previousEnd.AddMonths(1))
        };
    }
}
