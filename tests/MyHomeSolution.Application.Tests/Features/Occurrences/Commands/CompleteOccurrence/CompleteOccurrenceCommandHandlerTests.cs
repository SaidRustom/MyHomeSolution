using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Commands.CompleteOccurrence;

public sealed class CompleteOccurrenceCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public CompleteOccurrenceCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("completing-user");
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 3, 15, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldMarkOccurrenceAsCompleted()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            Notes = "All done!"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.Completed);
        occurrence.Notes.Should().Be("All done!");
    }

    [Fact]
    public async Task Handle_ShouldSetCompletionDetails()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand { OccurrenceId = occurrenceId };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.CompletedAt.Should().Be(new DateTimeOffset(2025, 3, 15, 10, 0, 0, TimeSpan.Zero));
        occurrence.CompletedByUserId.Should().Be("completing-user");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand { OccurrenceId = Guid.CreateVersion7() };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOccurrenceIsDeleted()
    {
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 1, 1),
            Status = OccurrenceStatus.Pending,
            IsDeleted = true
        };
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(occurrence);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand { OccurrenceId = occurrence.Id };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowNullNotes()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            Notes = null
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Notes.Should().BeNull();
        occurrence.Status.Should().Be(OccurrenceStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldPublishOccurrenceCompletedEvent()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand { OccurrenceId = occurrenceId };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<OccurrenceCompletedEvent>(e =>
                e.OccurrenceId == occurrenceId &&
                e.CompletedByUserId == "completing-user"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotPublishEvent_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new CompleteOccurrenceCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);
        var command = new CompleteOccurrenceCommand { OccurrenceId = Guid.CreateVersion7() };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<OccurrenceCompletedEvent>(), Arg.Any<CancellationToken>());
    }

    private async Task<Guid> SeedPendingOccurrence()
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
            DueDate = new DateOnly(2025, 3, 10),
            Status = OccurrenceStatus.Pending,
            AssignedToUserId = "user-1"
        };
        context.HouseholdTasks.Add(task);
        context.TaskOccurrences.Add(occurrence);
        await context.SaveChangesAsync();
        return occurrence.Id;
    }

    public void Dispose() => _factory.Dispose();
}
