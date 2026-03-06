using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public CreateNotificationCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("sender-user-id");
    }

    [Fact]
    public async Task Handle_ShouldCreateNotification_WithAllProperties()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "Task assigned to you",
            Description = "You have been assigned the kitchen cleaning task.",
            Type = NotificationType.TaskAssigned,
            ToUserId = "recipient-user-id",
            RelatedEntityId = Guid.CreateVersion7(),
            RelatedEntityType = "HouseholdTask"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync(n => n.Id == id);
        notification.Title.Should().Be("Task assigned to you");
        notification.Description.Should().Be("You have been assigned the kitchen cleaning task.");
        notification.Type.Should().Be(NotificationType.TaskAssigned);
        notification.FromUserId.Should().Be("sender-user-id");
        notification.ToUserId.Should().Be("recipient-user-id");
        notification.RelatedEntityId.Should().Be(command.RelatedEntityId);
        notification.RelatedEntityType.Should().Be("HouseholdTask");
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNewNotificationId()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "Test notification",
            Type = NotificationType.General,
            ToUserId = "recipient-user-id"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldSetFromUserId_FromCurrentUser()
    {
        _currentUserService.UserId.Returns("custom-sender");

        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "From sender",
            Type = NotificationType.General,
            ToUserId = "recipient"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync(n => n.Id == id);
        notification.FromUserId.Should().Be("custom-sender");
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenNoCurrentUser()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "Should fail",
            Type = NotificationType.General,
            ToUserId = "recipient"
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldPublishNotificationCreatedEvent()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "Event test",
            Type = NotificationType.TaskAssigned,
            ToUserId = "target-user"
        };

        var id = await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<NotificationCreatedEvent>(e =>
                e.NotificationId == id &&
                e.Title == "Event test" &&
                e.ToUserId == "target-user"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateNotification_WithOptionalFieldsNull()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateNotificationCommandHandler(context, _currentUserService, _publisher);
        var command = new CreateNotificationCommand
        {
            Title = "Minimal notification",
            Type = NotificationType.General,
            ToUserId = "recipient",
            Description = null,
            RelatedEntityId = null,
            RelatedEntityType = null
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync(n => n.Id == id);
        notification.Description.Should().BeNull();
        notification.RelatedEntityId.Should().BeNull();
        notification.RelatedEntityType.Should().BeNull();
    }

    public void Dispose() => _factory.Dispose();
}
