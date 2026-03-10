using FluentAssertions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Services;
namespace MyHomeSolution.Infrastructure.Tests.Services;

public sealed class TaskProcessingLockTests
{
    private readonly TaskProcessingLock _lock = new();

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnLockHandle_WhenNotContended()
    {
        var taskId = Guid.CreateVersion7();

        await using var handle = await _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(1));

        handle.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenLockAlreadyHeld()
    {
        var taskId = Guid.CreateVersion7();

        await using var first = await _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(1));
        first.Should().NotBeNull();

        var second = await _lock.TryAcquireAsync(taskId, TimeSpan.FromMilliseconds(50));
        second.Should().BeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSucceed_AfterPreviousLockReleased()
    {
        var taskId = Guid.CreateVersion7();

        var first = await _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(1));
        first.Should().NotBeNull();
        await first!.DisposeAsync();

        await using var second = await _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(1));
        second.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldAllowDifferentTasks_Concurrently()
    {
        var taskId1 = Guid.CreateVersion7();
        var taskId2 = Guid.CreateVersion7();

        await using var handle1 = await _lock.TryAcquireAsync(taskId1, TimeSpan.FromSeconds(1));
        await using var handle2 = await _lock.TryAcquireAsync(taskId2, TimeSpan.FromSeconds(1));

        handle1.Should().NotBeNull();
        handle2.Should().NotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldRespectCancellation()
    {
        var taskId = Guid.CreateVersion7();

        await using var first = await _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(1));
        first.Should().NotBeNull();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        var act = () => _lock.TryAcquireAsync(taskId, TimeSpan.FromSeconds(30), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

public sealed class OccurrenceGeneratorServiceTests
{
    [Fact]
    public void TopUp_ShouldCreateExpectedCount()
    {
        var task = CreateRecurringTask(today: new DateOnly(2025, 6, 1));
        var dbContext = NSubstitute.Substitute.For<IApplicationDbContext>();

        var count = OccurrenceReconciler.TopUp(
            task, new DateOnly(2025, 6, 1), dbContext, requiredFutureOccurrences: 5);

        count.Should().Be(5);
        task.Occurrences.Should().HaveCount(5);
    }

    [Fact]
    public void TopUp_ShouldNotExceedEndDate()
    {
        var task = CreateRecurringTask(
            today: new DateOnly(2025, 6, 1),
            endDate: new DateOnly(2025, 6, 10));
        var dbContext = NSubstitute.Substitute.For<IApplicationDbContext>();

        var count = OccurrenceReconciler.TopUp(
            task, new DateOnly(2025, 6, 1), dbContext, requiredFutureOccurrences: 50);

        task.Occurrences.Should().AllSatisfy(o => o.DueDate.Should().BeBefore(new DateOnly(2025, 6, 11)));
    }

    [Fact]
    public void TopUp_ShouldSkipWhenEnoughPendingExist()
    {
        var task = CreateRecurringTask(today: new DateOnly(2025, 6, 1));
        for (var i = 0; i < 5; i++)
        {
            task.Occurrences.Add(new TaskOccurrence
            {
                HouseholdTaskId = task.Id,
                DueDate = new DateOnly(2025, 6, 1).AddDays(i + 1),
                Status = OccurrenceStatus.Pending
            });
        }

        var dbContext = NSubstitute.Substitute.For<IApplicationDbContext>();
        var count = OccurrenceReconciler.TopUp(
            task, new DateOnly(2025, 6, 1), dbContext, requiredFutureOccurrences: 5);

        count.Should().Be(0);
    }

    [Fact]
    public void TopUp_ShouldRotateAssignees()
    {
        var task = CreateRecurringTask(today: new DateOnly(2025, 6, 1));
        var dbContext = NSubstitute.Substitute.For<IApplicationDbContext>();

        OccurrenceReconciler.TopUp(
            task, new DateOnly(2025, 6, 1), dbContext, requiredFutureOccurrences: 4);

        var assignees = task.Occurrences.Select(o => o.AssignedToUserId).ToList();
        assignees[0].Should().Be("user-a");
        assignees[1].Should().Be("user-b");
        assignees[2].Should().Be("user-a");
        assignees[3].Should().Be("user-b");
    }

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
            DueDate = new DateOnly(2025, 6, 1),
            Status = OccurrenceStatus.Pending,
            AssignedToUserId = "user-a"
        };

        var bill = OccurrenceReconciler.CreateBillForOccurrence(
            task, occurrence, ["user-a", "user-b"]);

        bill.Splits.Should().HaveCount(2);
        bill.Splits.Sum(s => s.Percentage).Should().Be(100m);
        bill.Amount.Should().Be(100m);
    }

    private static HouseholdTask CreateRecurringTask(
        DateOnly today, DateOnly? endDate = null)
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
            StartDate = today,
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
}
