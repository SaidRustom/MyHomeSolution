using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IOccurrenceScheduler _occurrenceScheduler = Substitute.For<IOccurrenceScheduler>();

    public UpdateTaskCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("test-user");
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingTask()
    {
        var taskId = await SeedTask("Original Title", TaskPriority.Low, TaskCategory.General);

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Updated Title",
            Description = "New description",
            Priority = TaskPriority.Critical,
            Category = TaskCategory.Maintenance,
            EstimatedDurationMinutes = 120,
            IsActive = true,
            DueDate = new DateOnly(2025, 8, 1),
            AssignedToUserId = "user-2"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == taskId);
        task.Title.Should().Be("Updated Title");
        task.Description.Should().Be("New description");
        task.Priority.Should().Be(TaskPriority.Critical);
        task.Category.Should().Be(TaskCategory.Maintenance);
        task.EstimatedDurationMinutes.Should().Be(120);
        task.DueDate.Should().Be(new DateOnly(2025, 8, 1));
        task.AssignedToUserId.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = Guid.CreateVersion7(),
            Title = "Nonexistent",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskIsSoftDeleted()
    {
        var taskId = await SeedDeletedTask();

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Should not update",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowDeactivatingTask()
    {
        var taskId = await SeedTask("Active Task", TaskPriority.Medium, TaskCategory.Cleaning);

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Active Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsActive = false
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == taskId);
        task.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldPublishTaskUpdatedEvent()
    {
        var taskId = await SeedTask("Original Title", TaskPriority.Low, TaskCategory.General);

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "New Title",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<TaskUpdatedEvent>(e => e.TaskId == taskId && e.Title == "New Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotPublishEvent_WhenTaskDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = Guid.CreateVersion7(),
            Title = "Nonexistent",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<TaskUpdatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUpdateAutoBillConfig()
    {
        var taskId = await SeedRecurringTask("Bill Task");

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Bill Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["user-a"],
            AutoCreateBill = true,
            DefaultBillAmount = 50m,
            DefaultBillCurrency = "USD",
            DefaultBillCategory = BillCategory.Utilities,
            DefaultBillTitle = "Weekly Utilities"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == taskId);
        task.AutoCreateBill.Should().BeTrue();
        task.DefaultBillAmount.Should().Be(50m);
        task.DefaultBillCurrency.Should().Be("USD");
        task.DefaultBillCategory.Should().Be(BillCategory.Utilities);
        task.DefaultBillTitle.Should().Be("Weekly Utilities");
    }

    [Fact]
    public async Task Handle_ShouldCallRegenerateOccurrences_WhenRecurrenceChanges()
    {
        var taskId = await SeedRecurringTask("Recurring Task");

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Recurring Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Monthly,
            Interval = 2,
            RecurrenceStartDate = new DateOnly(2025, 6, 1),
            AssigneeUserIds = ["user-a"]
        };

        await handler.Handle(command, CancellationToken.None);

        await _occurrenceScheduler.Received(1)
            .SyncOccurrencesAsync(taskId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCallRegenerate_WhenRecurrenceUnchanged()
    {
        var taskId = await SeedRecurringTask("Same Task");

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Updated Title Only",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["user-a"]
        };

        await handler.Handle(command, CancellationToken.None);

        await _occurrenceScheduler.DidNotReceiveWithAnyArgs()
            .SyncOccurrencesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateRecurrencePattern_WhenSwitchingToRecurring()
    {
        var taskId = await SeedTask("Non-Recurring", TaskPriority.Low, TaskCategory.General);

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _currentUserService, _occurrenceScheduler, _publisher);
        var command = new UpdateTaskCommand
        {
            Id = taskId,
            Title = "Now Recurring",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsActive = true,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 7, 1),
            AssigneeUserIds = ["user-1", "user-2"]
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees)
            .FirstAsync(t => t.Id == taskId);
        task.IsRecurring.Should().BeTrue();
        task.RecurrencePattern.Should().NotBeNull();
        task.RecurrencePattern!.Type.Should().Be(RecurrenceType.Daily);
        task.RecurrencePattern.Assignees.Should().HaveCount(2);

        await _occurrenceScheduler.Received(1)
            .SyncOccurrencesAsync(taskId, Arg.Any<CancellationToken>());
    }

    private async Task<Guid> SeedTask(string title, TaskPriority priority, TaskCategory category)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = title,
            Priority = priority,
            Category = category,
            IsActive = true,
            DueDate = new DateOnly(2025, 6, 1)
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> SeedRecurringTask(string title)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = title,
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true
        };
        var pattern = new RecurrencePattern
        {
            HouseholdTaskId = task.Id,
            Type = RecurrenceType.Weekly,
            Interval = 1,
            StartDate = new DateOnly(2025, 1, 1)
        };
        pattern.Assignees.Add(new RecurrenceAssignee
        {
            RecurrencePatternId = pattern.Id,
            UserId = "user-a",
            Order = 0
        });
        task.RecurrencePattern = pattern;
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private async Task<Guid> SeedDeletedTask()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Deleted Task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsDeleted = true,
            IsActive = false
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    public void Dispose() => _factory.Dispose();
}
