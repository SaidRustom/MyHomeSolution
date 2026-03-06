using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Infrastructure.Hubs;
using MyHomeSolution.Infrastructure.Services;
using NSubstitute;

namespace MyHomeSolution.Infrastructure.Tests.Services;

public sealed class SignalRNotificationServiceTests
{
    private readonly IHubContext<TaskHub> _hubContext = Substitute.For<IHubContext<TaskHub>>();
    private readonly IHubContext<NotificationHub> _notificationHubContext = Substitute.For<IHubContext<NotificationHub>>();
    private readonly IClientProxy _allClientsProxy = Substitute.For<IClientProxy>();
    private readonly IClientProxy _groupProxy = Substitute.For<IClientProxy>();
    private readonly SignalRNotificationService _sut;

    public SignalRNotificationServiceTests()
    {
        _hubContext.Clients.All.Returns(_allClientsProxy);
        _sut = new SignalRNotificationService(_hubContext, _notificationHubContext);
    }

    [Fact]
    public async Task SendTaskNotificationAsync_ShouldBroadcastToAllClients()
    {
        var notification = new TaskNotification
        {
            EventType = "TaskCreatedEvent",
            TaskId = Guid.CreateVersion7(),
            Title = "New task",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _sut.SendTaskNotificationAsync(notification, CancellationToken.None);

        await _allClientsProxy.Received(1).SendCoreAsync(
            SignalRNotificationService.TaskNotificationMethod,
            Arg.Is<object[]>(args => args.Length == 1 && args[0] == notification),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOccurrenceNotificationAsync_ShouldSendToCorrectTaskGroup()
    {
        var taskId = Guid.CreateVersion7();
        var expectedGroup = TaskHub.FormatGroupName(taskId);
        _hubContext.Clients.Group(expectedGroup).Returns(_groupProxy);

        var notification = new OccurrenceNotification
        {
            EventType = "OccurrenceCompletedEvent",
            OccurrenceId = Guid.CreateVersion7(),
            TaskId = taskId,
            CompletedByUserId = "user-1",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _sut.SendOccurrenceNotificationAsync(notification, CancellationToken.None);

        _hubContext.Clients.Received(1).Group(expectedGroup);
        await _groupProxy.Received(1).SendCoreAsync(
            SignalRNotificationService.OccurrenceNotificationMethod,
            Arg.Is<object[]>(args => args.Length == 1 && args[0] == notification),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOccurrenceNotificationAsync_ShouldFormatGroupNameConsistently()
    {
        var taskId = Guid.CreateVersion7();
        var expectedGroup = $"task-{taskId}";
        _hubContext.Clients.Group(expectedGroup).Returns(_groupProxy);

        var notification = new OccurrenceNotification
        {
            EventType = "OccurrenceSkippedEvent",
            OccurrenceId = Guid.CreateVersion7(),
            TaskId = taskId,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _sut.SendOccurrenceNotificationAsync(notification, CancellationToken.None);

        TaskHub.FormatGroupName(taskId).Should().Be(expectedGroup);
        _hubContext.Clients.Received(1).Group(expectedGroup);
    }

    [Fact]
    public async Task SendTaskNotificationAsync_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var notification = new TaskNotification
        {
            EventType = "TaskDeletedEvent",
            TaskId = Guid.CreateVersion7(),
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _sut.SendTaskNotificationAsync(notification, token);

        await _allClientsProxy.Received(1).SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            token);
    }

    [Fact]
    public async Task SendUserNotificationAsync_ShouldSendToCorrectUserGroup()
    {
        var userId = "target-user-id";
        var expectedGroup = NotificationHub.FormatGroupName(userId);
        var userGroupProxy = Substitute.For<IClientProxy>();
        _notificationHubContext.Clients.Group(expectedGroup).Returns(userGroupProxy);

        var notification = new UserPushNotification
        {
            EventType = "NotificationCreatedEvent",
            NotificationId = Guid.CreateVersion7(),
            Title = "New notification",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _sut.SendUserNotificationAsync(userId, notification, CancellationToken.None);

        _notificationHubContext.Clients.Received(1).Group(expectedGroup);
        await userGroupProxy.Received(1).SendCoreAsync(
            SignalRNotificationService.UserNotificationMethod,
            Arg.Is<object[]>(args => args.Length == 1 && args[0] == notification),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendUserNotificationAsync_ShouldFormatGroupNameConsistently()
    {
        var userId = "some-user-123";
        var expectedGroup = $"user-{userId}";

        NotificationHub.FormatGroupName(userId).Should().Be(expectedGroup);
    }
}
