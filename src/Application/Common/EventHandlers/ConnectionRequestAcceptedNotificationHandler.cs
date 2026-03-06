using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ConnectionRequestAcceptedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider,
    IIdentityService identityService)
    : INotificationHandler<ConnectionRequestAcceptedEvent>
{
    public async Task Handle(ConnectionRequestAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var acceptedByName = await identityService.GetUserNameByIdAsync(notification.AcceptedByUserId, cancellationToken)
            ?? "Someone";

        var entity = new Notification
        {
            Title = $"{acceptedByName} accepted your connection request",
            Description = $"{acceptedByName} has accepted your connection request.",
            Type = NotificationType.ConnectionRequestAccepted,
            FromUserId = notification.AcceptedByUserId,
            ToUserId = notification.RequesterId,
            RelatedEntityId = notification.ConnectionId,
            RelatedEntityType = "UserConnection"
        };

        dbContext.Notifications.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimeService.SendUserNotificationAsync(
            entity.ToUserId,
            new UserPushNotification
            {
                EventType = nameof(NotificationCreatedEvent),
                NotificationId = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                RelatedEntityId = entity.RelatedEntityId,
                RelatedEntityType = entity.RelatedEntityType,
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
