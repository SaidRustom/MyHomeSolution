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

public sealed class TaskUpdatedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public TaskUpdatedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyAssignee_WhenDifferentFromUpdater()
    {
        var task = await SeedTask("owner-id", "assignee-id", lastModifiedBy: "owner-id");

        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskUpdatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "assignee-id");
        notification.Should().NotBeNull();
        notification!.Type.Should().Be(NotificationType.TaskUpdated);
        notification.FromUserId.Should().Be("owner-id");
    }

    [Fact]
    public async Task Handle_ShouldNotifyOwner_WhenAssigneeUpdates()
    {
        var task = await SeedTask("owner-id", "assignee-id", lastModifiedBy: "assignee-id");

        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskUpdatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "owner-id");
        notification.Should().NotBeNull();
        notification!.FromUserId.Should().Be("assignee-id");
    }

    [Fact]
    public async Task Handle_ShouldNotifyBothOwnerAndAssignee_WhenThirdPartyUpdates()
    {
        var task = await SeedTask("owner-id", "assignee-id", lastModifiedBy: "third-party");

        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskUpdatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Select(n => n.ToUserId).Should().Contain(["owner-id", "assignee-id"]);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenOwnerUpdatesSelfAssigned()
    {
        var task = await SeedTask("user-1", "user-1", lastModifiedBy: "user-1");

        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskUpdatedEvent(task.Id, task.Title), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeForEachRecipient()
    {
        var task = await SeedTask("owner-id", "assignee-id", lastModifiedBy: "third-party");

        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskUpdatedEvent(task.Id, task.Title), CancellationToken.None);

        await _realtimeService.Received(2).SendUserNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<UserPushNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenTaskNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new TaskUpdatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var act = () => handler.Handle(
            new TaskUpdatedEvent(Guid.CreateVersion7(), "Missing"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<HouseholdTask> SeedTask(string createdBy, string? assignedTo, string lastModifiedBy)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Updated task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            CreatedBy = createdBy,
            AssignedToUserId = assignedTo,
            LastModifiedBy = lastModifiedBy
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _factory.Dispose();
}
