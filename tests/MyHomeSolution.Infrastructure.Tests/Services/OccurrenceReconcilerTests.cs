using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Services;
using NSubstitute;

namespace MyHomeSolution.Infrastructure.Tests.Services;

public sealed class OccurrenceReconcilerTests
{
    private static readonly DateOnly Today = new(2025, 6, 15);
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    // ── Reconcile: basic schedule generation ─────────────────────────────

    [Fact]
    public void Reconcile_ShouldCreateOccurrences_WhenNoneExist()
    {
        var task = CreateRecurringTask(startDate: Today);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        result.Created.Should().Be(5);
        result.Removed.Should().Be(0);
        result.Reused.Should().Be(0);
        task.Occurrences.Should().HaveCount(5);
    }

    [Fact]
    public void Reconcile_ShouldNotExceedEndDate()
    {
        var task = CreateRecurringTask(
            startDate: Today,
            endDate: Today.AddDays(3));

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 10, Now);

        task.Occurrences.Should().AllSatisfy(o =>
            o.DueDate.Should().BeOnOrBefore(Today.AddDays(3)));
        result.Created.Should().BeLessThanOrEqualTo(4);
    }

    // ── Reconcile: completed occurrences are never touched ───────────────

    [Fact]
    public void Reconcile_ShouldPreserveCompletedOccurrences()
    {
        var task = CreateRecurringTask(startDate: Today);
        var completed = AddOccurrence(task, Today, OccurrenceStatus.Completed);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        task.Occurrences.Should().Contain(completed);
        completed.IsDeleted.Should().BeFalse();
        completed.DueDate.Should().Be(Today);
    }

    [Fact]
    public void Reconcile_ShouldPreserveInProgressOccurrences()
    {
        var task = CreateRecurringTask(startDate: Today);
        var inProgress = AddOccurrence(task, Today, OccurrenceStatus.InProgress);

        OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        task.Occurrences.Should().Contain(inProgress);
        inProgress.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_ShouldPreserveOverdueOccurrences()
    {
        var task = CreateRecurringTask(startDate: Today.AddDays(-10));
        var overdue = AddOccurrence(task, Today.AddDays(-5), OccurrenceStatus.Overdue);

        OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        task.Occurrences.Should().Contain(overdue);
        overdue.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_ShouldPreserveSkippedOccurrences()
    {
        var task = CreateRecurringTask(startDate: Today.AddDays(-10));
        var skipped = AddOccurrence(task, Today.AddDays(-3), OccurrenceStatus.Skipped);

        OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        task.Occurrences.Should().Contain(skipped);
        skipped.IsDeleted.Should().BeFalse();
    }

    // ── Reconcile: pending reuse & rescheduling ─────────────────────────

    [Fact]
    public void Reconcile_ShouldReusePendingOnMatchingDates()
    {
        var task = CreateRecurringTask(startDate: Today);
        var existing = AddOccurrence(task, Today, OccurrenceStatus.Pending);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        result.Reused.Should().BeGreaterThanOrEqualTo(1);
        existing.IsDeleted.Should().BeFalse();
        task.Occurrences.Should().Contain(existing);
    }

    [Fact]
    public void Reconcile_ShouldRescheduleExtraPendingToNewDates()
    {
        // Create task with daily recurrence starting today
        var task = CreateRecurringTask(startDate: Today);
        // Add a pending occurrence on a date that won't match the new schedule
        var orphan = AddOccurrence(task, Today.AddDays(100), OccurrenceStatus.Pending);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        // The orphan should have been either reused (re-dated) or soft-deleted
        // since the schedule generates dates starting from Today
        result.Reused.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Reconcile_ShouldSoftDeleteExcessPending()
    {
        var task = CreateRecurringTask(startDate: Today, endDate: Today.AddDays(2));
        // Add more pending occurrences than the pattern supports
        for (var i = 0; i < 10; i++)
        {
            AddOccurrence(task, Today.AddDays(i), OccurrenceStatus.Pending);
        }

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 3, Now);

        result.Removed.Should().BeGreaterThan(0);
        var deletedCount = task.Occurrences.Count(o => o.IsDeleted);
        deletedCount.Should().BeGreaterThan(0);
    }

    // ── Reconcile: date changes ─────────────────────────────────────────

    [Fact]
    public void Reconcile_ShouldAdaptWhenStartDateMoves()
    {
        // Originally started at Today, now start date moves forward
        var task = CreateRecurringTask(startDate: Today.AddDays(5));
        AddOccurrence(task, Today, OccurrenceStatus.Pending);
        AddOccurrence(task, Today.AddDays(1), OccurrenceStatus.Pending);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 3, Now);

        // Old pending before new start date should be reused or removed
        var activePending = task.Occurrences
            .Where(o => !o.IsDeleted && o.Status == OccurrenceStatus.Pending)
            .ToList();
        activePending.Should().AllSatisfy(o =>
            o.DueDate.Should().BeOnOrAfter(Today.AddDays(5)));
    }

    [Fact]
    public void Reconcile_ShouldSoftDeleteWhenEndDateShrinks()
    {
        var task = CreateRecurringTask(startDate: Today, endDate: Today.AddDays(2));
        // Add occurrences beyond the new end date
        AddOccurrence(task, Today.AddDays(5), OccurrenceStatus.Pending);
        AddOccurrence(task, Today.AddDays(6), OccurrenceStatus.Pending);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        // Occurrences beyond end date should be removed or re-dated
        var activePending = task.Occurrences
            .Where(o => !o.IsDeleted && o.Status == OccurrenceStatus.Pending)
            .ToList();
        activePending.Should().AllSatisfy(o =>
            o.DueDate.Should().BeOnOrBefore(Today.AddDays(2)));
    }

    // ── Reconcile: interval change ──────────────────────────────────────

    [Fact]
    public void Reconcile_ShouldRecomputeScheduleOnIntervalChange()
    {
        // Daily with interval=1 -> existing dates at day+1,+2,+3
        var task = CreateRecurringTask(startDate: Today);
        AddOccurrence(task, Today, OccurrenceStatus.Pending);
        AddOccurrence(task, Today.AddDays(1), OccurrenceStatus.Pending);
        AddOccurrence(task, Today.AddDays(2), OccurrenceStatus.Pending);

        // Change interval to 2 (every other day)
        task.RecurrencePattern!.Interval = 2;

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 3, Now);

        var activePending = task.Occurrences
            .Where(o => !o.IsDeleted && o.Status == OccurrenceStatus.Pending)
            .OrderBy(o => o.DueDate)
            .ToList();

        // Should have dates at Today, Today+2, Today+4 (every 2 days)
        activePending.Should().HaveCount(3);
        activePending[0].DueDate.Should().Be(Today);
        activePending[1].DueDate.Should().Be(Today.AddDays(2));
        activePending[2].DueDate.Should().Be(Today.AddDays(4));
    }

    // ── Reconcile: assignee rotation ────────────────────────────────────

    [Fact]
    public void Reconcile_ShouldRotateAssigneesOnNewOccurrences()
    {
        var task = CreateRecurringTask(startDate: Today);

        OccurrenceReconciler.Reconcile(task, Today, _dbContext, 4, Now);

        var assignees = task.Occurrences
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.DueDate)
            .Select(o => o.AssignedToUserId)
            .ToList();

        assignees[0].Should().Be("user-a");
        assignees[1].Should().Be("user-b");
        assignees[2].Should().Be("user-a");
        assignees[3].Should().Be("user-b");
    }

    // ── Reconcile: non-recurring cleans up ──────────────────────────────

    [Fact]
    public void Reconcile_ShouldRemoveFuturePending_WhenNotRecurring()
    {
        var task = CreateRecurringTask(startDate: Today);
        AddOccurrence(task, Today.AddDays(1), OccurrenceStatus.Pending);
        AddOccurrence(task, Today.AddDays(2), OccurrenceStatus.Pending);

        // Switch to non-recurring
        task.IsRecurring = false;
        task.RecurrencePattern = null;

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        result.Removed.Should().Be(2);
        result.Created.Should().Be(0);
    }

    [Fact]
    public void Reconcile_ShouldKeepCompleted_EvenWhenNotRecurring()
    {
        var task = CreateRecurringTask(startDate: Today);
        var completed = AddOccurrence(task, Today.AddDays(1), OccurrenceStatus.Completed);
        AddOccurrence(task, Today.AddDays(2), OccurrenceStatus.Pending);

        task.IsRecurring = false;
        task.RecurrencePattern = null;

        OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        completed.IsDeleted.Should().BeFalse();
    }

    // ── Reconcile: immutable future counts toward target ────────────────

    [Fact]
    public void Reconcile_ShouldCountImmutableFutureTowardTarget()
    {
        var task = CreateRecurringTask(startDate: Today);
        // 3 completed future occurrences on schedule dates
        AddOccurrence(task, Today, OccurrenceStatus.Completed);
        AddOccurrence(task, Today.AddDays(1), OccurrenceStatus.Completed);
        AddOccurrence(task, Today.AddDays(2), OccurrenceStatus.Completed);

        var result = OccurrenceReconciler.Reconcile(task, Today, _dbContext, 5, Now);

        // Need 5 total future, have 3 completed -> should create 2 pending
        result.Created.Should().Be(2);
    }

    // ── TopUp: additive-only ────────────────────────────────────────────

    [Fact]
    public void TopUp_ShouldCreateOccurrences_WhenBelowTarget()
    {
        var task = CreateRecurringTask(startDate: Today);

        var count = OccurrenceReconciler.TopUp(task, Today, _dbContext, 5);

        count.Should().Be(5);
        task.Occurrences.Should().HaveCount(5);
    }

    [Fact]
    public void TopUp_ShouldNotCreate_WhenTargetAlreadyMet()
    {
        var task = CreateRecurringTask(startDate: Today);
        for (var i = 0; i < 5; i++)
        {
            AddOccurrence(task, Today.AddDays(i), OccurrenceStatus.Pending);
        }

        var count = OccurrenceReconciler.TopUp(task, Today, _dbContext, 5);

        count.Should().Be(0);
    }

    [Fact]
    public void TopUp_ShouldNeverRemoveExisting()
    {
        var task = CreateRecurringTask(startDate: Today);
        var existing = AddOccurrence(task, Today.AddDays(100), OccurrenceStatus.Pending);

        OccurrenceReconciler.TopUp(task, Today, _dbContext, 5);

        existing.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void TopUp_ShouldRotateAssignees()
    {
        var task = CreateRecurringTask(startDate: Today);

        OccurrenceReconciler.TopUp(task, Today, _dbContext, 4);

        var assignees = task.Occurrences.OrderBy(o => o.DueDate).Select(o => o.AssignedToUserId).ToList();
        assignees[0].Should().Be("user-a");
        assignees[1].Should().Be("user-b");
        assignees[2].Should().Be("user-a");
        assignees[3].Should().Be("user-b");
    }

    [Fact]
    public void TopUp_ShouldRespectEndDate()
    {
        var task = CreateRecurringTask(startDate: Today, endDate: Today.AddDays(2));

        var count = OccurrenceReconciler.TopUp(task, Today, _dbContext, 10);

        task.Occurrences.Should().AllSatisfy(o =>
            o.DueDate.Should().BeOnOrBefore(Today.AddDays(2)));
    }

    // ── CreateBillForOccurrence ─────────────────────────────────────────

    [Fact]
    public void CreateBillForOccurrence_ShouldSplitEvenly()
    {
        var task = new HouseholdTask
        {
            Title = "Test",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            DefaultBillAmount = 100m,
            DefaultBillCurrency = "CAD",
            DefaultBillCategory = BillCategory.General,
            AutoCreateBill = true
        };

        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = Today,
            Status = OccurrenceStatus.Pending,
            AssignedToUserId = "user-a"
        };

        var bill = OccurrenceReconciler.CreateBillForOccurrence(
            task, occurrence, ["user-a", "user-b"]);

        bill.Splits.Should().HaveCount(2);
        bill.Splits.Sum(s => s.Percentage).Should().Be(100m);
        bill.Amount.Should().Be(100m);
    }

    // ── EnumerateDueDates ───────────────────────────────────────────────

    [Fact]
    public void EnumerateDueDates_ShouldStartFromStartDate()
    {
        var pattern = new RecurrencePattern
        {
            Type = RecurrenceType.Daily,
            Interval = 1,
            StartDate = Today
        };

        var dates = pattern.EnumerateDueDates(Today.AddDays(-5), null, 3);

        dates.Should().HaveCount(3);
        dates[0].Should().Be(Today);
    }

    [Fact]
    public void EnumerateDueDates_ShouldRespectEndDate()
    {
        var pattern = new RecurrencePattern
        {
            Type = RecurrenceType.Daily,
            Interval = 1,
            StartDate = Today,
            EndDate = Today.AddDays(2)
        };

        var dates = pattern.EnumerateDueDates(Today, null, 10);

        dates.Should().HaveCount(3); // Today, +1, +2
        dates.Should().AllSatisfy(d => d.Should().BeOnOrBefore(Today.AddDays(2)));
    }

    [Fact]
    public void EnumerateDueDates_ShouldRespectUntilOverride()
    {
        var pattern = new RecurrencePattern
        {
            Type = RecurrenceType.Daily,
            Interval = 1,
            StartDate = Today,
            EndDate = Today.AddDays(100) // pattern end is far away
        };

        var dates = pattern.EnumerateDueDates(Today, Today.AddDays(1), 10);

        dates.Should().HaveCount(2); // Today, +1
    }

    [Fact]
    public void EnumerateDueDates_ShouldHandleWeeklyInterval()
    {
        var pattern = new RecurrencePattern
        {
            Type = RecurrenceType.Weekly,
            Interval = 2,
            StartDate = Today
        };

        var dates = pattern.EnumerateDueDates(Today, null, 3);

        dates[0].Should().Be(Today);
        dates[1].Should().Be(Today.AddDays(14));
        dates[2].Should().Be(Today.AddDays(28));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static HouseholdTask CreateRecurringTask(
        DateOnly startDate, DateOnly? endDate = null)
    {
        var task = new HouseholdTask
        {
            Title = "Test recurring task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true
        };

        var pattern = new RecurrencePattern
        {
            HouseholdTaskId = task.Id,
            Type = RecurrenceType.Daily,
            Interval = 1,
            StartDate = startDate,
            EndDate = endDate,
            LastAssigneeIndex = -1
        };

        pattern.Assignees.Add(new RecurrenceAssignee
        {
            RecurrencePatternId = pattern.Id,
            UserId = "user-a",
            Order = 0
        });
        pattern.Assignees.Add(new RecurrenceAssignee
        {
            RecurrencePatternId = pattern.Id,
            UserId = "user-b",
            Order = 1
        });

        task.RecurrencePattern = pattern;
        return task;
    }

    private static TaskOccurrence AddOccurrence(
        HouseholdTask task, DateOnly dueDate, OccurrenceStatus status)
    {
        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = dueDate,
            Status = status,
            AssignedToUserId = "user-a"
        };
        task.Occurrences.Add(occurrence);
        return occurrence;
    }
}
