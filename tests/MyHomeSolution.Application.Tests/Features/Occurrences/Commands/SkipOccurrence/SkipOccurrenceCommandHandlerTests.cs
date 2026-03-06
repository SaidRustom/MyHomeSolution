using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Features.Occurrences.Commands.SkipOccurrence;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Occurrences.Commands.SkipOccurrence;

public sealed class SkipOccurrenceCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    [Fact]
    public async Task Handle_ShouldMarkOccurrenceAsSkipped()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            Notes = "Away on vacation"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.Skipped);
        occurrence.Notes.Should().Be("Away on vacation");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand { OccurrenceId = Guid.CreateVersion7() };

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
            DueDate = new DateOnly(2025, 2, 1),
            Status = OccurrenceStatus.Pending,
            IsDeleted = true
        };
        seedContext.HouseholdTasks.Add(task);
        seedContext.TaskOccurrences.Add(occurrence);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand { OccurrenceId = occurrence.Id };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowNullNotes()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand
        {
            OccurrenceId = occurrenceId,
            Notes = null
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var occurrence = await assertContext.TaskOccurrences.FirstAsync(o => o.Id == occurrenceId);
        occurrence.Status.Should().Be(OccurrenceStatus.Skipped);
        occurrence.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldPublishOccurrenceSkippedEvent()
    {
        var occurrenceId = await SeedPendingOccurrence();

        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand { OccurrenceId = occurrenceId };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<OccurrenceSkippedEvent>(e => e.OccurrenceId == occurrenceId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotPublishEvent_WhenOccurrenceDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new SkipOccurrenceCommandHandler(context, _publisher);
        var command = new SkipOccurrenceCommand { OccurrenceId = Guid.CreateVersion7() };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<OccurrenceSkippedEvent>(), Arg.Any<CancellationToken>());
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
            DueDate = new DateOnly(2025, 2, 10),
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
