using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class NotificationCreatedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public NotificationCreatedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendUserNotification_WithCorrectData()
    {
        var handler = new NotificationCreatedEventHandler(_notificationService, _dateTimeProvider);
        var @event = new NotificationCreatedEvent(Guid.CreateVersion7(), "Task assigned", null, "target-user", null, null);

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendUserNotificationAsync(
            "target-user",
            Arg.Is<UserPushNotification>(n =>
                n.EventType == nameof(NotificationCreatedEvent) &&
                n.NotificationId == @event.NotificationId &&
                n.Title == "Task assigned" &&
                n.OccurredAt == new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        var handler = new NotificationCreatedEventHandler(_notificationService, _dateTimeProvider);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(
            new NotificationCreatedEvent(Guid.CreateVersion7(), "Test", null, "user-1", null, null), token);

        await _notificationService.Received(1).SendUserNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<UserPushNotification>(),
            token);
    }

    [Fact]
    public async Task Handle_ShouldSendToCorrectUserId()
    {
        var handler = new NotificationCreatedEventHandler(_notificationService, _dateTimeProvider);
        var @event = new NotificationCreatedEvent(Guid.CreateVersion7(), "Notification", null, "specific-user-123", null, null);

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendUserNotificationAsync(
            "specific-user-123",
            Arg.Any<UserPushNotification>(),
            Arg.Any<CancellationToken>());
    }
}
