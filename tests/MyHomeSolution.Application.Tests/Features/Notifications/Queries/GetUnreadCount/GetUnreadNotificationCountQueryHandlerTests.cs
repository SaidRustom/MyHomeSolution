using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Queries.GetUnreadCount;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadNotificationCountQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetUnreadNotificationCountQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectUnreadCount()
    {
        await SeedNotifications("user-1", unread: 4, read: 2);

        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var count = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenAllAreRead()
    {
        await SeedNotifications("user-1", unread: 0, read: 5);

        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var count = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenNoNotifications()
    {
        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var count = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotCountOtherUsersNotifications()
    {
        await SeedNotifications("user-1", unread: 2, read: 0);
        await SeedNotifications("other-user", unread: 5, read: 0);

        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var count = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedNotifications()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.Notifications.Add(new Notification
        {
            Title = "Deleted unread",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1",
            IsRead = false,
            IsDeleted = true
        });
        seedContext.Notifications.Add(new Notification
        {
            Title = "Active unread",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1",
            IsRead = false
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var count = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetUnreadNotificationCountQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task SeedNotifications(string toUserId, int unread, int read)
    {
        using var context = _factory.CreateContext();
        for (var i = 0; i < unread; i++)
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
        for (var i = 0; i < read; i++)
        {
            context.Notifications.Add(new Notification
            {
                Title = $"Read {i}",
                Type = NotificationType.General,
                FromUserId = "sender",
                ToUserId = toUserId,
                IsRead = true,
                ReadAt = DateTimeOffset.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
