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

public sealed class ConnectionRequestSentNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ConnectionRequestSentNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldPersistNotification_ForAddressee()
    {
        using var context = _factory.CreateContext();
        var handler = new ConnectionRequestSentNotificationHandler(
            context, _realtimeService, _dateTimeProvider);
        var connectionId = Guid.CreateVersion7();

        await handler.Handle(
            new ConnectionRequestSentEvent(connectionId, "requester", "addressee"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications
            .FirstOrDefaultAsync(n => n.ToUserId == "addressee");
        notification.Should().NotBeNull();
        notification!.Title.Should().Contain("connection request");
        notification.Type.Should().Be(NotificationType.ConnectionRequestReceived);
        notification.FromUserId.Should().Be("requester");
        notification.ToUserId.Should().Be("addressee");
        notification.RelatedEntityId.Should().Be(connectionId);
        notification.RelatedEntityType.Should().Be("UserConnection");
    }

    [Fact]
    public async Task Handle_ShouldSendRealtimePushNotification()
    {
        using var context = _factory.CreateContext();
        var handler = new ConnectionRequestSentNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ConnectionRequestSentEvent(Guid.CreateVersion7(), "requester", "addressee"),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "addressee",
            Arg.Is<UserPushNotification>(n =>
                n.Title!.Contains("connection request") &&
                n.EventType == nameof(NotificationCreatedEvent)),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();
}
