using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Commands.MarkAsRead;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkNotificationAsReadCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTimeOffset _now = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(_now);
    }

    [Fact]
    public async Task Handle_ShouldMarkNotificationAsRead()
    {
        var notificationId = await SeedNotification("user-1");

        using var context = _factory.CreateContext();
        var handler = new MarkNotificationAsReadCommandHandler(context, _currentUserService, _dateTimeProvider);

        await handler.Handle(new MarkNotificationAsReadCommand(notificationId), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync(n => n.Id == notificationId);
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAlreadyRead()
    {
        var notificationId = await SeedNotification("user-1", isRead: true);

        using var context = _factory.CreateContext();
        var handler = new MarkNotificationAsReadCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new MarkNotificationAsReadCommand(notificationId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new MarkNotificationAsReadCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new MarkNotificationAsReadCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationBelongsToAnotherUser()
    {
        var notificationId = await SeedNotification("other-user");

        using var context = _factory.CreateContext();
        var handler = new MarkNotificationAsReadCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new MarkNotificationAsReadCommand(notificationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new MarkNotificationAsReadCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(
            new MarkNotificationAsReadCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task<Guid> SeedNotification(string toUserId, bool isRead = false)
    {
        using var context = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Test notification",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = toUserId,
            IsRead = isRead,
            ReadAt = isRead ? _now : null
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification.Id;
    }

    public void Dispose() => _factory.Dispose();
}
