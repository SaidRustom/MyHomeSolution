using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class OccurrenceCompletedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public OccurrenceCompletedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 7, 10, 16, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyTaskOwner_WhenCompletedByDifferentUser()
    {
        var (task, occurrence) = await SeedOccurrence("task-owner", "completer-id");

        using var context = _factory.CreateContext();
        var handler = new OccurrenceCompletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceCompletedEvent(occurrence.Id, task.Id, "completer-id"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "task-owner");
        notification.Should().NotBeNull();
        notification!.Title.Should().Be("Occurrence completed");
        notification.Description.Should().Contain("Test task");
        notification.Type.Should().Be(NotificationType.OccurrenceCompleted);
        notification.FromUserId.Should().Be("completer-id");
        notification.RelatedEntityId.Should().Be(task.Id);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenOwnerCompletesOwn()
    {
        var (task, occurrence) = await SeedOccurrence("owner-id", "owner-id");

        using var context = _factory.CreateContext();
        var handler = new OccurrenceCompletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceCompletedEvent(occurrence.Id, task.Id, "owner-id"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenCompletedByIsNull()
    {
        var (task, occurrence) = await SeedOccurrence("owner-id", null);

        using var context = _factory.CreateContext();
        var handler = new OccurrenceCompletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceCompletedEvent(occurrence.Id, task.Id, null),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        var (task, occurrence) = await SeedOccurrence("task-owner", "completer");

        using var context = _factory.CreateContext();
        var handler = new OccurrenceCompletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new OccurrenceCompletedEvent(occurrence.Id, task.Id, "completer"),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "task-owner",
            Arg.Is<UserPushNotification>(n => n.Title == "Occurrence completed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenOccurrenceNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new OccurrenceCompletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var act = () => handler.Handle(
            new OccurrenceCompletedEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), "user"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<(HouseholdTask task, TaskOccurrence occurrence)> SeedOccurrence(
        string taskOwner, string? completedBy)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Test task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            IsRecurring = true,
            CreatedBy = taskOwner
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();

        var occurrence = new TaskOccurrence
        {
            HouseholdTaskId = task.Id,
            DueDate = new DateOnly(2025, 7, 10),
            Status = OccurrenceStatus.Completed,
            CompletedByUserId = completedBy
        };
        context.TaskOccurrences.Add(occurrence);
        await context.SaveChangesAsync();

        return (task, occurrence);
    }

    public void Dispose() => _factory.Dispose();
}
