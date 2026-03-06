using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OccurrenceScheduler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<OccurrenceGeneratorOptions> options,
    ILogger<OccurrenceScheduler> logger)
    : IOccurrenceScheduler
{
    private readonly int _requiredFutureOccurrences = options.Value.RequiredFutureOccurrences;

    public async Task RegenerateOccurrencesAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var today = dateTimeProvider.Today;

        var task = await dbContext.HouseholdTasks
            .IgnoreQueryFilters()
            .Where(t => t.Id == taskId && !t.IsDeleted)
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees.OrderBy(a => a.Order))
            .Include(t => t.Occurrences.Where(o => !o.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found for regeneration", taskId);
            return;
        }

        // Remove all future Pending occurrences (not InProgress, Completed, Skipped, Overdue)
        var futurePending = task.Occurrences
            .Where(o => o.DueDate >= today && o.Status == OccurrenceStatus.Pending)
            .ToList();

        foreach (var occurrence in futurePending)
        {
            // If occurrence has a linked bill, soft-delete it
            if (occurrence.BillId.HasValue)
            {
                var bill = await dbContext.Bills.FindAsync([occurrence.BillId.Value], cancellationToken);
                if (bill is not null)
                {
                    bill.IsDeleted = true;
                    bill.DeletedAt = dateTimeProvider.UtcNow;
                }
            }

            occurrence.IsDeleted = true;
            occurrence.DeletedAt = dateTimeProvider.UtcNow;
        }

        logger.LogInformation(
            "Removed {Count} future pending occurrences for task {TaskId}",
            futurePending.Count, taskId);

        if (!task.IsRecurring || task.RecurrencePattern is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Reset assignee index for clean regeneration
        task.RecurrencePattern.LastAssigneeIndex = -1;

        // Regenerate occurrences using the shared logic
        var generated = OccurrenceGeneratorService.GenerateOccurrencesForTask(
            task, today, dbContext, _requiredFutureOccurrences);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Regenerated {Count} occurrences for task {TaskId}",
            generated, taskId);
    }
}
