using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Queries.GetNotificationById;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Queries.GetNotificationById;

public sealed class GetNotificationByIdQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetNotificationByIdQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotificationDetail()
    {
        var relatedEntityId = Guid.CreateVersion7();
        using var seedContext = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Task assigned",
            Description = "Kitchen cleaning has been assigned to you.",
            Type = NotificationType.TaskAssigned,
            FromUserId = "sender-id",
            ToUserId = "user-1",
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = "HouseholdTask",
            IsRead = false
        };
        seedContext.Notifications.Add(notification);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetNotificationByIdQueryHandler(context, _currentUserService);

        var result = await handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        result.Id.Should().Be(notification.Id);
        result.Title.Should().Be("Task assigned");
        result.Description.Should().Be("Kitchen cleaning has been assigned to you.");
        result.Type.Should().Be(NotificationType.TaskAssigned);
        result.FromUserId.Should().Be("sender-id");
        result.ToUserId.Should().Be("user-1");
        result.RelatedEntityId.Should().Be(relatedEntityId);
        result.RelatedEntityType.Should().Be("HouseholdTask");
        result.IsRead.Should().BeFalse();
        result.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationDoesNotExist()
    {
        using var context = _factory.CreateContext();
        var handler = new GetNotificationByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetNotificationByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenBelongsToAnotherUser()
    {
        using var seedContext = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Other user's notification",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "other-user"
        };
        seedContext.Notifications.Add(notification);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetNotificationByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenNotificationIsDeleted()
    {
        using var seedContext = _factory.CreateContext();
        var notification = new Notification
        {
            Title = "Deleted notification",
            Type = NotificationType.General,
            FromUserId = "sender",
            ToUserId = "user-1",
            IsDeleted = true
        };
        seedContext.Notifications.Add(notification);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetNotificationByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetNotificationByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetNotificationByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    public void Dispose() => _factory.Dispose();
}
