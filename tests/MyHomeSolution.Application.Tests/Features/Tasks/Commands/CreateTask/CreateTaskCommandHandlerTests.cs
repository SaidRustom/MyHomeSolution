using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    [Fact]
    public async Task Handle_ShouldCreateNonRecurringTask()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Clean kitchen",
            Description = "Deep clean all surfaces",
            Priority = TaskPriority.High,
            Category = TaskCategory.Cleaning,
            EstimatedDurationMinutes = 30,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 6, 15),
            AssignedToUserId = "user-1"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == id);
        task.Title.Should().Be("Clean kitchen");
        task.Description.Should().Be("Deep clean all surfaces");
        task.Priority.Should().Be(TaskPriority.High);
        task.Category.Should().Be(TaskCategory.Cleaning);
        task.EstimatedDurationMinutes.Should().Be(30);
        task.IsRecurring.Should().BeFalse();
        task.DueDate.Should().Be(new DateOnly(2025, 6, 15));
        task.AssignedToUserId.Should().Be("user-1");
        task.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCreateRecurringTask_WithRecurrencePattern()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Vacuum house",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            Interval = 2,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            RecurrenceEndDate = new DateOnly(2025, 12, 31),
            AssigneeUserIds = ["user-a", "user-b"]
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks
            .Include(t => t.RecurrencePattern!)
            .ThenInclude(rp => rp.Assignees)
            .FirstAsync(t => t.Id == id);

        task.IsRecurring.Should().BeTrue();
        task.RecurrencePattern.Should().NotBeNull();
        task.RecurrencePattern!.Type.Should().Be(RecurrenceType.Weekly);
        task.RecurrencePattern.Interval.Should().Be(2);
        task.RecurrencePattern.StartDate.Should().Be(new DateOnly(2025, 1, 1));
        task.RecurrencePattern.EndDate.Should().Be(new DateOnly(2025, 12, 31));
        task.RecurrencePattern.Assignees.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldSetAssigneeOrder_Correctly()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Rotating task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily,
            Interval = 1,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["alice", "bob", "charlie"]
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var pattern = await assertContext.RecurrencePatterns
            .Include(rp => rp.Assignees)
            .FirstAsync(rp => rp.HouseholdTask.Id == id);

        var assignees = pattern.Assignees.OrderBy(a => a.Order).ToList();
        assignees[0].UserId.Should().Be("alice");
        assignees[0].Order.Should().Be(0);
        assignees[1].UserId.Should().Be("bob");
        assignees[1].Order.Should().Be(1);
        assignees[2].UserId.Should().Be("charlie");
        assignees[2].Order.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateRecurrencePattern_WhenNotRecurring()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "One-off task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 3, 1)
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks
            .Include(t => t.RecurrencePattern)
            .FirstAsync(t => t.Id == id);
        task.RecurrencePattern.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNewTaskId()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Test task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = false,
            DueDate = new DateOnly(2025, 3, 1)
        };

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultInterval_WhenIntervalIsNull()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Default interval task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily,
            Interval = null,
            RecurrenceStartDate = new DateOnly(2025, 1, 1),
            AssigneeUserIds = ["user-1"]
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var pattern = await assertContext.RecurrencePatterns
            .FirstAsync(rp => rp.HouseholdTask.Id == id);
        pattern.Interval.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldPublishTaskCreatedEvent()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateTaskCommandHandler(context, _publisher);
        var command = new CreateTaskCommand
        {
            Title = "Event test task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = false
        };

        var id = await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<TaskCreatedEvent>(e => e.TaskId == id && e.Title == "Event test task"),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();
}
