using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Commands.RescheduleOccurrence;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Commands.RescheduleOccurrence;

public sealed class RescheduleOccurrenceCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public RescheduleOccurrenceCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("rescheduling-user");
    }

    [Fact]
    public async Task Handle_ShouldUpdateDueDate()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Pending, new DateOnly(2025, 6, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = new DateOnly(2025, 7, 15),
            Notes = "Moved to July"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.DueDate.Should().Be(new DateOnly(2025, 7, 15));
        occurrence.Notes.Should().Be("Moved to July");
    }

    [Fact]
    public async Task Handle_ShouldResetOverdueToStatusPending_WhenRescheduledToFuture()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Overdue, new DateOnly(2025, 1, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldAllowReschedulingInProgressOccurrence()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.InProgress, new DateOnly(2025, 6, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = new DateOnly(2025, 8, 1)
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.DueDate.Should().Be(new DateOnly(2025, 8, 1));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenCompleted()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Completed, new DateOnly(2025, 6, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = new DateOnly(2025, 8, 1)
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenSkipped()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Skipped, new DateOnly(2025, 6, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = new DateOnly(2025, 8, 1)
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = Guid.CreateVersion7(),
            NewDueDate = new DateOnly(2025, 8, 1)
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldPublishOccurrenceRescheduledEvent()
    {
        var occurrenceId = await SeedOccurrence(OccurrenceStatus.Pending, new DateOnly(2025, 6, 1));

        using var context = _factory.CreateContext();
        var handler = new RescheduleOccurrenceCommandHandler(context, _currentUserService, _publisher);
        var command = new RescheduleOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            NewDueDate = new DateOnly(2025, 7, 15)
        };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<OccurrenceRescheduledEvent>(e =>
                e.OccurrenceId == occurrenceId &&
                e.PreviousDate == new DateOnly(2025, 6, 1) &&
                e.NewDate == new DateOnly(2025, 7, 15) &&
                e.RescheduledByUserId == "rescheduling-user"),
            Arg.Any<CancellationToken>());
    }

    private async Task<Guid> SeedOccurrence(OccurrenceStatus status, DateOnly dueDate)
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
            DueDate = dueDate,
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
