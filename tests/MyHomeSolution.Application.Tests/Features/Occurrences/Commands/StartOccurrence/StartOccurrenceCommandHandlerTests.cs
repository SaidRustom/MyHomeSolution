using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Commands.StartOccurrence;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Commands.StartOccurrence;

public sealed class StartOccurrenceCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public StartOccurrenceCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("starting-user");
    }

    [Fact]
    public async Task Handle_ShouldMarkOccurrenceAsInProgress()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            Notes = "Starting now"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.InProgress);
        occurrence.Notes.Should().Be("Starting now");
    }

    [Fact]
    public async Task Handle_ShouldAllowStartingOverdueOccurrence()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Overdue);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = occurrenceId };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.InProgress);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenAlreadyCompleted()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Completed);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = occurrenceId };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenSkipped()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Skipped);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = occurrenceId };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenAlreadyInProgress()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.InProgress);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = occurrenceId };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = Guid.CreateVersion7() };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldPublishOccurrenceStartedEvent()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Pending);

        using var context = _factory.CreateContext();
        var handler = new StartOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new StartOccurrenceCommand { OccurrenceId = occurrenceId };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<OccurrenceStartedEvent>(e =>
                e.OccurrenceId == occurrenceId &&
                e.StartedByUserId == "starting-user"),
            Arg.Any<CancellationToken>());
    }

    private async Task<Guid> SeedOccurrence(OccurrenceStatus status)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Parent Task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            IsActive = true
        };
        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 6, 1),
            Status = status,
            AssignedToUserId = "user-a"
        };
        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.Add(occurrence);
        await context.SaveChangesAsync();
        return occurrence.Id;
    }

    public void Dispose() => _factory.Dispose();
}
