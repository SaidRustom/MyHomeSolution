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
    ITaskProcessingLock processingLock,
    IOptions<OccurrenceGeneratorOptions> options,
    ILogger<OccurrenceScheduler> logger)
    : IOccurrenceScheduler
{
    private readonly int _requiredFutureOccurrences = options.Value.RequiredFutureOccurrences;
    private const int MaxRetries = 3;

    public async Task SyncOccurrencesAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await processingLock.TryAcquireAsync(
            taskId, TimeSpan.FromSeconds(3), cancellationToken)
            ?? throw new InvalidOperationException(
                $"Could not acquire processing lock for task {taskId}. Another operation is in progress.");

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await SyncOccurrencesCoreAsync(taskId, cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(
                        "Concurrency conflict on entity {EntityType}",
                        entry.Metadata.Name);

                    var proposedValues = entry.CurrentValues;
                    var originalValues = entry.OriginalValues;

                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);

                    if (databaseValues == null)
                    {
                        logger.LogError("Entity was deleted by another process.");
                        continue;
                    }

                    foreach (var property in proposedValues.Properties)
                    {
                        var proposed = proposedValues[property];
                        var original = originalValues[property];
                        var database = databaseValues[property];

                        if (!Equals(proposed, database))
                        {
                            logger.LogError(
                                "Property {Property}: Original={Original}, Proposed={Proposed}, Database={Database}",
                                property.Name,
                                original,
                                proposed,
                                database);
                        }
                    }
                }
                    logger.LogWarning(ex,
                    "Concurrency conflict syncing occurrences for task {TaskId} (attempt {Attempt}/{MaxRetries})",
                    taskId, attempt, MaxRetries);

                if (attempt == MaxRetries)
                    throw;

                await Task.Delay(100 * attempt, cancellationToken);
            }
        }
    }

    private async Task SyncOccurrencesCoreAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.Today;
        var now = dateTimeProvider.UtcNow;

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
            logger.LogWarning("Task {TaskId} not found for occurrence sync", taskId);
            return;
        }

        // Soft-delete linked bills for any occurrences the reconciler will remove
        var pendingBillIds = task.Occurrences
            .Where(o => o.Status == OccurrenceStatus.Pending && o.BillId.HasValue)
            .Select(o => o.BillId!.Value)
            .ToList();

        var result = OccurrenceReconciler.Reconcile(
            task, today, dbContext, _requiredFutureOccurrences, now);

        // After reconciliation, find bills that belong to now-deleted occurrences
        if (pendingBillIds.Count > 0)
        {
            var deletedOccurrenceBillIds = task.Occurrences
                .Where(o => o.IsDeleted && o.BillId.HasValue)
                .Select(o => o.BillId!.Value)
                .Where(pendingBillIds.Contains)
                .ToHashSet();

            if (deletedOccurrenceBillIds.Count > 0)
            {
                var billsToDelete = await dbContext.Bills
                    .Where(b => deletedOccurrenceBillIds.Contains(b.Id) && !b.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var bill in billsToDelete)
                {
                    bill.IsDeleted = true;
                    bill.DeletedAt = now;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occurrence sync for task {TaskId}: {Created} created, {Removed} removed, {Reused} reused",
            taskId, result.Created, result.Removed, result.Reused);
    }
}
