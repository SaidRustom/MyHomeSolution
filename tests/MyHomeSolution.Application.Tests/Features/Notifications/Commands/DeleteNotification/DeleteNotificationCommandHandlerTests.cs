using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Commands.DeleteNotification;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Commands.DeleteNotification;

public sealed class DeleteNotificationCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public DeleteNotificationCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteNotification()
    {
        var notificationId = await SeedNotification("user-1");

        using var context = _factory.CreateContext();
        var handler = new DeleteNotificationCommandHandler(context, _currentUserService);

        await handler.Handle(new DeleteNotificationCommand(notificationId), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications
            .IgnoreQueryFilters()
            .FirstAsync(n => n.Id == notificationId);
        notification.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteNotificationCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new DeleteNotificationCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationBelongsToAnotherUser()
    {
        var notificationId = await SeedNotification("other-user");

        using var context = _factory.CreateContext();
        var handler = new DeleteNotificationCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new DeleteNotificationCommand(notificationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAlreadyDeleted()
    {
        using var seedContext = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Already deleted",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1",
            IsDeleted = true
        };
        seedContext.Notifications.Add(notification);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new DeleteNotificationCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new DeleteNotificationCommand(notification.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new DeleteNotificationCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new DeleteNotificationCommand(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task<Guid> SeedNotification(string toUserId)
    {
        using var context = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Notification to delete",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = toUserId
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification.Id;
    }

    public void Dispose() => _factory.Dispose();
}
