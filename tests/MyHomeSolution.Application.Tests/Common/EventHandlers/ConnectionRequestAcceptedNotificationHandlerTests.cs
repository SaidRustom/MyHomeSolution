using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class ConnectionRequestAcceptedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ConnectionRequestAcceptedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldPersistNotification_ForOriginalRequester()
    {
        using var context = _factory.CreateContext();
        var handler = new ConnectionRequestAcceptedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);
        var connectionId = Guid.CreateVersion7();

        await handler.Handle(
            new ConnectionRequestAcceptedEvent(connectionId, "requester", "accepter"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications
            .FirstOrDefaultAsync(n => n.ToUserId == "requester");
        notification.Should().NotBeNull();
        notification!.Title.Should().Contain("accepted");
        notification.Type.Should().Be(NotificationType.ConnectionRequestAccepted);
        notification.FromUserId.Should().Be("accepter");
        notification.ToUserId.Should().Be("requester");
        notification.RelatedEntityId.Should().Be(connectionId);
        notification.RelatedEntityType.Should().Be("UserConnection");
    }

    [Fact]
    public async Task Handle_ShouldSendRealtimePushNotification_ToRequester()
    {
        using var context = _factory.CreateContext();
        var handler = new ConnectionRequestAcceptedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ConnectionRequestAcceptedEvent(Guid.CreateVersion7(), "requester", "accepter"),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "requester",
            Arg.Is<UserPushNotification>(n =>
                n.Title!.Contains("accepted") &&
                n.EventType == nameof(NotificationCreatedEvent)),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();
}
