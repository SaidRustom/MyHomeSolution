using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Commands.DeleteTask;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public DeleteTaskCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("test-user");
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        _dateTimeProvider.Today.Returns(new DateOnly(2025, 6, 1));
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteTask()
    {
        var taskId = await SeedTask();

        using var context = _factory.CreateContext();
        var handler = new DeleteTaskCommandHandler(context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new DeleteTaskCommand(taskId), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var task = await assertContext.HouseholdTasks
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == taskId);
        task.IsDeleted.Should().BeTrue();
        task.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteTaskCommandHandler(context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(
            new DeleteTaskCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTaskIsAlreadyDeleted()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Already deleted",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsDeleted = true,
            IsActive = false
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new DeleteTaskCommandHandler(context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(
            new DeleteTaskCommand(task.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldPublishTaskDeletedEvent()
    {
        var taskId = await SeedTask();

        using var context = _factory.CreateContext();
        var handler = new DeleteTaskCommandHandler(context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new DeleteTaskCommand(taskId), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<TaskDeletedEvent>(e => e.TaskId == taskId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotPublishEvent_WhenTaskDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteTaskCommandHandler(context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(
            new DeleteTaskCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<TaskDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    private async Task<Guid> SeedTask()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Task to delete",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsActive = true
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    public void Dispose() => _factory.Dispose();
}
