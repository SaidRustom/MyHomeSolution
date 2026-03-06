using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
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

    [Fact]
    public async Task Handle_ShouldUpdateExistingTask()
    {
        var taskId = await SeedTask("Original Title", TaskPriority.Low, TaskCategory.General);

        using var context = _factory.CreateContext();
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
        var handler = new UpdateTaskCommandHandler(context, _publisher);
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
