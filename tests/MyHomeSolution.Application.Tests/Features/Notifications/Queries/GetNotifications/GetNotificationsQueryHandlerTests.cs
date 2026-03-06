using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Queries.GetNotifications;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetNotificationsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedNotifications_ForCurrentUser()
    {
        await SeedNotifications("user-1", 5);
        await SeedNotifications("other-user", 3);

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_ShouldRespectPagination()
    {
        await SeedNotifications("user-1", 5);

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsRead()
    {
        await SeedNotificationsWithReadState("user-1", unreadCount: 3, readCount: 2);

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 20, IsRead = false };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
    }

    [Fact]
    public async Task Handle_ShouldFilterByType()
    {
        await SeedNotificationsWithTypes("user-1");

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery
        {
            PageNumber = 1,
            PageSize = 20,
            Type = NotificationType.TaskAssigned
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().AllSatisfy(n => n.Type.Should().Be(NotificationType.TaskAssigned));
    }

    [Fact]
    public async Task Handle_ShouldOrderByNewestFirst()
    {
        using var seedContext = _factory.CreateContext();
        var first = new Notification
        {
            Title = "First created",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1"
        };
        seedContext.Notifications.Add(first);
        await seedContext.SaveChangesAsync();

        var second = new Notification
        {
            Title = "Second created",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1"
        };
        seedContext.Notifications.Add(second);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.First().Title.Should().Be("Second created");
        result.Items.Last().Title.Should().Be("First created");
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedNotifications()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.Notifications.Add(new Notification
        {
            Title = "Deleted",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1",
            IsDeleted = true
        });
        seedContext.Notifications.Add(new Notification
        {
            Title = "Active",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1"
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.First().Title.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetNotificationsQuery { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoNotifications()
    {
        using var context = _factory.CreateContext();
        var handler = new GetNotificationsQueryHandler(context, _currentUserService);
        var query = new GetNotificationsQuery { PageNumber = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    private async Task SeedNotifications(string toUserId, int count)
    {
        using var context = _factory.CreateContext();
        for (var i = 0; i < count; i++)
        {
            context.Notifications.Add(new Notification
            {
                Title = $"Notification {i}",
                Type = NotificationType.General,
                FromUserId = "sender",
                ToUserId = toUserId
            });
        }
        await context.SaveChangesAsync();
    }

    private async Task SeedNotificationsWithReadState(string toUserId, int unreadCount, int readCount)
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
                ReadAt = DateTimeOffset.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    private async Task SeedNotificationsWithTypes(string toUserId)
    {
        using var context = _factory.CreateContext();
        context.Notifications.Add(new Notification
        {
            Title = "Assigned",
            Type = NotificationType.TaskAssigned,
            FromUserId = "sender",
            ToUserId = toUserId
        });
        context.Notifications.Add(new Notification
        {
            Title = "Updated",
            Type = NotificationType.TaskUpdated,
            FromUserId = "sender",
            ToUserId = toUserId
        });
        context.Notifications.Add(new Notification
        {
            Title = "Share",
            Type = NotificationType.ShareReceived,
            FromUserId = "sender",
            ToUserId = toUserId
        });
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
