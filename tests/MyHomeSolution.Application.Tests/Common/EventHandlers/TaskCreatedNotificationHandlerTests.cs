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

public sealed class TaskCreatedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public TaskCreatedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldPersistNotification_WhenTaskAssignedToDifferentUser()
    {
        var task = await SeedTask("creator-id", "assignee-id");

        using var context = _factory.CreateContext();
        var handler = new TaskCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskCreatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "assignee-id");
        notification.Should().NotBeNull();
        notification!.Title.Should().Contain("Clean kitchen");
        notification.Type.Should().Be(NotificationType.TaskAssigned);
        notification.FromUserId.Should().Be("creator-id");
        notification.RelatedEntityId.Should().Be(task.Id);
        notification.RelatedEntityType.Should().Be("HouseholdTask");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        var task = await SeedTask("creator-id", "assignee-id");

        using var context = _factory.CreateContext();
        var handler = new TaskCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskCreatedEvent(task.Id, task.Title), CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "assignee-id",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("Clean kitchen")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenSelfAssigned()
    {
        var task = await SeedTask("user-1", "user-1");

        using var context = _factory.CreateContext();
        var handler = new TaskCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskCreatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenNoAssignee()
    {
        var task = await SeedTask("creator-id", assignedTo: null);

        using var context = _factory.CreateContext();
        var handler = new TaskCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskCreatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenTaskNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new TaskCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var act = () => handler.Handle(
            new TaskCreatedEvent(Guid.CreateVersion7(), "Missing"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<HouseholdTask> SeedTask(string createdBy, string? assignedTo)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Clean kitchen",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning,
            CreatedBy = createdBy,
            AssignedToUserId = assignedTo
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _factory.Dispose();
}
