using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Infrastructure.Services;

/// <summary>
/// Pure, deterministic reconciliation logic for task occurrences.
/// Computes the diff between the desired schedule and existing occurrences,
/// then applies the minimal set of changes: reuse pending, soft-delete extras,
/// create missing. Completed/InProgress/Overdue occurrences are never touched.
/// </summary>
internal static class OccurrenceReconciler
{
    /// <summary>
    /// Result of a reconciliation pass — used for logging and testing.
    /// </summary>
    internal readonly record struct ReconcileResult(int Created, int Removed, int Reused);

    /// <summary>
    /// Reconciles the occurrences collection on <paramref name="task"/> to match
    /// the current <see cref="RecurrencePattern"/>.
    /// </summary>
    /// <param name="task">
    /// The fully-loaded task including <c>RecurrencePattern.Assignees</c> and
    /// <c>Occurrences</c> (non-deleted only).
    /// </param>
    /// <param name="today">Reference date for "future" calculations.</param>
    /// <param name="dbContext">DbContext for adding/removing entities.</param>
    /// <param name="requiredFutureOccurrences">
    /// Minimum number of pending/in-progress future occurrences to maintain.
    /// </param>
    /// <param name="now">Current UTC timestamp for soft-delete stamps.</param>
    internal static ReconcileResult Reconcile(
        HouseholdTask task,
        DateOnly today,
        IApplicationDbContext dbContext,
        int requiredFutureOccurrences,
        DateTimeOffset now)
    {
        var pattern = task.RecurrencePattern;

        // ── Non-recurring or deactivated: remove all future pending ──────────
        if (!task.IsRecurring || pattern is null)
        {
            var removedCount = SoftDeleteFuturePending(task, today, dbContext, now);
            return new ReconcileResult(0, removedCount, 0);
        }

        // ── Compute the desired schedule ─────────────────────────────────────
        //
        // We need enough dates to cover:
        //   (a) existing completed/overdue/in-progress that fall on schedule, plus
        //   (b) `requiredFutureOccurrences` future pending slots.
        //
        // To avoid over-generation we first compute with a generous cap, then trim.

        var desiredDates = pattern.EnumerateDueDates(
            pattern.StartDate,
            pattern.EndDate,
            maxCount: requiredFutureOccurrences + task.Occurrences.Count + 10);

        var desiredDateSet = desiredDates.ToHashSet();

        // ── Classify existing occurrences ────────────────────────────────────

        // Immutable: these are never modified or deleted (user already acted on them)
        var immutable = task.Occurrences
            .Where(o => o.Status is OccurrenceStatus.Completed
                        or OccurrenceStatus.InProgress
                        or OccurrenceStatus.Overdue
                        or OccurrenceStatus.Skipped)
            .ToList();

        // Mutable: future pending occurrences we can reuse, move, or remove
        var mutablePending = task.Occurrences
            .Where(o => o.Status == OccurrenceStatus.Pending)
            .OrderBy(o => o.DueDate)
            .ToList();

        // Dates already occupied by immutable occurrences (don't duplicate)
        var immutableDates = immutable.Select(o => o.DueDate).ToHashSet();

        // Desired dates that still need an occurrence (excluding immutable)
        var unfilledDates = desiredDates
            .Where(d => d >= today && !immutableDates.Contains(d))
            .ToList();

        // Count immutable future as contributing toward the target
        var immutableFutureCount = immutable.Count(o => o.DueDate >= today);

        // We need this many more pending slots
        var pendingSlotsNeeded = Math.Max(0, requiredFutureOccurrences - immutableFutureCount);

        // Trim unfilled dates to what we actually need
        if (unfilledDates.Count > pendingSlotsNeeded)
            unfilledDates = unfilledDates[..pendingSlotsNeeded];

        var unfilledDateSet = unfilledDates.ToHashSet();

        // ── Match mutable pending to unfilled dates ──────────────────────────

        var reused = 0;
        var removed = 0;
        var matchedDates = new HashSet<DateOnly>();

        // First pass: keep any pending that already sits on a desired date
        foreach (var occ in mutablePending.ToList())
        {
            if (unfilledDateSet.Contains(occ.DueDate) && matchedDates.Add(occ.DueDate))
            {
                // Perfect match — keep it
                reused++;
                mutablePending.Remove(occ);
            }
        }

        // Dates still needing an occurrence after keeping perfect matches
        var datesToFill = unfilledDates.Where(d => !matchedDates.Contains(d)).ToList();

        // Second pass: reuse remaining pending by re-dating them
        var reuseQueue = new Queue<TaskOccurrence>(mutablePending);
        var toCreate = new List<DateOnly>();

        foreach (var date in datesToFill)
        {
            if (reuseQueue.TryDequeue(out var recycled))
            {
                recycled.DueDate = date;
                reused++;
            }
            else
            {
                toCreate.Add(date);
            }
        }

        // Anything left in the reuse queue is excess — soft-delete
        while (reuseQueue.TryDequeue(out var excess))
        {
            SoftDeleteOccurrence(excess, dbContext, now);
            removed++;
        }

        // Also soft-delete any pending that are before today (past pending that
        // the overdue service hasn't picked up yet but are no longer on schedule)
        foreach (var occ in task.Occurrences.Where(
            o => o.Status == OccurrenceStatus.Pending
                 && o.DueDate < today
                 && !desiredDateSet.Contains(o.DueDate)).ToList())
        {
            // Only if it wasn't already reused above
            if (!occ.IsDeleted)
            {
                SoftDeleteOccurrence(occ, dbContext, now);
                removed++;
            }
        }

        // ── Create new occurrences for remaining unfilled dates ──────────────

        var assignees = pattern.Assignees
            .OrderBy(a => a.Order)
            .Select(a => a.UserId)
            .ToList();

        var created = 0;

        foreach (var date in toCreate)
        {
            var assigneeUserId = pattern.GetNextAssigneeUserId();
            pattern.AdvanceAssigneeIndex();

            var occurrence = new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = date,
                Status = OccurrenceStatus.Pending,
                AssignedToUserId = assigneeUserId
            };

            if (task.AutoCreateBill && task.DefaultBillAmount.HasValue && assignees.Count > 0)
            {
                var bill = CreateBillForOccurrence(task, occurrence, assignees);
                dbContext.Bills.Add(bill);
                occurrence.BillId = bill.Id;
            }

            dbContext.TaskOccurrences.Add(occurrence);
            created++;
        }

        return new ReconcileResult(created, removed, reused);
    }

    /// <summary>
    /// Additive-only reconciliation used by the background generator.
    /// Never removes or re-dates — only creates occurrences to reach the target count.
    /// </summary>
    internal static int TopUp(
        HouseholdTask task,
        DateOnly today,
        IApplicationDbContext dbContext,
        int requiredFutureOccurrences)
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

            dbContext.TaskOccurrences.Add(occurrence);
            nextDate = pattern.GetNextOccurrenceDate(nextDate);
            created++;
        }

        return created;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int SoftDeleteFuturePending(
        HouseholdTask task, DateOnly today, IApplicationDbContext dbContext, DateTimeOffset now)
    {
        var futurePending = task.Occurrences
            .Where(o => o.DueDate >= today && o.Status == OccurrenceStatus.Pending)
            .ToList();

        foreach (var occurrence in futurePending)
            SoftDeleteOccurrence(occurrence, dbContext, now);

        return futurePending.Count;
    }

    private static void SoftDeleteOccurrence(
        TaskOccurrence occurrence, IApplicationDbContext dbContext, DateTimeOffset now)
    {
        if (occurrence.BillId.HasValue)
        {
            // Bill is in a different aggregate — can't navigate directly.
            // Mark it for deletion via a lightweight query in the caller.
            // For now, record the bill id on a list.
        }

        occurrence.IsDeleted = true;
        occurrence.DeletedAt = now;
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

    internal static DateOnly MaxDate(DateOnly a, DateOnly b) => a >= b ? a : b;
}
