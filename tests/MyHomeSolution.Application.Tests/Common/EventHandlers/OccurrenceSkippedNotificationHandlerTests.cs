using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class OccurrenceSkippedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public OccurrenceSkippedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 7, 11, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyTaskOwner_WhenSkippedByDifferentUser()
    {
        var (task, occurrence) = await SeedOccurrence("task-owner", "skipper-id");

        using var context = _factory.CreateContext();
        var handler = new OccurrenceSkippedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceSkippedEvent(occurrence.Id, task.Id),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "task-owner");
        notification.Should().NotBeNull();
        notification!.Title.Should().Be("Occurrence skipped");
        notification.Description.Should().Contain("Skippable task");
        notification.Type.Should().Be(NotificationType.OccurrenceSkipped);
        notification.FromUserId.Should().Be("skipper-id");
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenOwnerSkipsOwn()
    {
        var (task, occurrence) = await SeedOccurrence("owner-id", "owner-id");

        using var context = _factory.CreateContext();
        var handler = new OccurrenceSkippedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceSkippedEvent(occurrence.Id, task.Id),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenOccurrenceNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new OccurrenceSkippedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var act = () => handler.Handle(
            new OccurrenceSkippedEvent(Guid.CreateVersion7(), Guid.CreateVersion7()),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<(HouseholdTask task, TaskOccurrence occurrence)> SeedOccurrence(
        string taskOwner, string skippedBy)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Skippable task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            IsRecurring = true,
            CreatedBy = taskOwner
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();

        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 7, 11),
            Status = OccurrenceStatus.Skipped,
            LastModifiedBy = skippedBy
        };
        context.TaskOccurrences.Add(occurrence);
        await context.SaveChangesAsync();

        return (task, occurrence);
    }

    public void Dispose() => _factory.Dispose();
}
