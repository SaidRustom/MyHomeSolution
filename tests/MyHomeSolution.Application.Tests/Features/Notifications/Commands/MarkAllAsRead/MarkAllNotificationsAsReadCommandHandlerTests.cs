using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Commands.MarkAllAsRead;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllNotificationsAsReadCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTimeOffset _now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    public MarkAllNotificationsAsReadCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(_now);
    }

    [Fact]
    public async Task Handle_ShouldMarkAllUnreadNotificationsAsRead()
    {
        await SeedNotifications("user-1", unreadCount: 3, readCount: 2);

        using var context = _factory.CreateContext();
        var handler = new MarkAllNotificationsAsReadCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var count = await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        count.Should().Be(3);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications
            .Where(n => n.ToUserId == "user-1")
            .ToListAsync();
        notifications.Should().AllSatisfy(n =>
        {
            n.IsRead.Should().BeTrue();
            n.ReadAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenNoUnreadNotifications()
    {
        await SeedNotifications("user-1", unreadCount: 0, readCount: 3);

        using var context = _factory.CreateContext();
        var handler = new MarkAllNotificationsAsReadCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var count = await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotAffectOtherUsersNotifications()
    {
        await SeedNotifications("user-1", unreadCount: 2, readCount: 0);
        await SeedNotifications("other-user", unreadCount: 3, readCount: 0);

        using var context = _factory.CreateContext();
        var handler = new MarkAllNotificationsAsReadCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var otherUserUnread = await assertContext.Notifications
            .CountAsync(n => n.ToUserId == "other-user" && !n.IsRead);
        otherUserUnread.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldSetReadAtToCurrentTime()
    {
        await SeedNotifications("user-1", unreadCount: 1, readCount: 0);

        using var context = _factory.CreateContext();
        var handler = new MarkAllNotificationsAsReadCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync(n => n.ToUserId == "user-1");
        notification.ReadAt.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new MarkAllNotificationsAsReadCommandHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task SeedNotifications(string toUserId, int unreadCount, int readCount)
    {
        using var context = _factory.CreateContext();
        for (var i = 0; i < unreadCount; i++)
        {
            context.Notifications.Add(new Notification
            {
                Title = $"Unread {i}",
                Type = NotificationType.General,
                FromUserId = "sender",
                ToUserId = toUserId,
                IsRead = false
            });
        }
        for (var i = 0; i < readCount; i++)
        {
            context.Notifications.Add(new Notification
            {
                Title = $"Read {i}",
                Type = NotificationType.General,
                FromUserId = "sender",
                ToUserId = toUserId,
                IsRead = true,
                ReadAt = _now.AddHours(-1)
            });
        }
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
