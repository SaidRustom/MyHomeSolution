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

public sealed class BillDeletedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public BillDeletedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyAffectedUsers()
    {
        using var context = _factory.CreateContext();
        var handler = new BillDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var billId = Guid.CreateVersion7();
        await handler.Handle(
            new BillDeletedEvent(billId, "Groceries", "user-1", ["user-2", "user-3"]),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Should().AllSatisfy(n =>
        {
            n.Type.Should().Be(NotificationType.BillDeleted);
            n.FromUserId.Should().Be("user-1");
            n.Title.Should().Contain("Bill deleted");
        });
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotifications()
    {
        using var context = _factory.CreateContext();
        var handler = new BillDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillDeletedEvent(Guid.CreateVersion7(), "Test", "user-1", ["user-2"]),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "user-2",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("Bill deleted")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotifications_WhenNoAffectedUsers()
    {
        using var context = _factory.CreateContext();
        var handler = new BillDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillDeletedEvent(Guid.CreateVersion7(), "Test", "user-1", []),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    public void Dispose() => _factory.Dispose();
}
