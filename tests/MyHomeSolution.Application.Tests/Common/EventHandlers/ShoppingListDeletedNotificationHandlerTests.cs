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

public sealed class ShoppingListDeletedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ShoppingListDeletedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyAllSharedUsers()
    {
        using var context = _factory.CreateContext();
        var handler = new ShoppingListDeletedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        var shoppingListId = Guid.CreateVersion7();
        await handler.Handle(
            new ShoppingListDeletedEvent(shoppingListId, "Groceries", "user-1", ["user-2", "user-3"]),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Should().AllSatisfy(n =>
        {
            n.Type.Should().Be(NotificationType.ShoppingListDeleted);
            n.FromUserId.Should().Be("user-1");
        });
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotifications()
    {
        using var context = _factory.CreateContext();
        var handler = new ShoppingListDeletedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingListDeletedEvent(Guid.CreateVersion7(), "Test", "user-1", ["user-2"]),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "user-2",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("deleted")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenNoSharedUsers()
    {
        using var context = _factory.CreateContext();
        var handler = new ShoppingListDeletedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingListDeletedEvent(Guid.CreateVersion7(), "Test", "user-1", []),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    public void Dispose() => _factory.Dispose();
}
