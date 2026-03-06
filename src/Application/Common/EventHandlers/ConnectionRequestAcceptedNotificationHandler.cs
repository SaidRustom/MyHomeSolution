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
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ConnectionRequestAcceptedEvent>
{
    public async Task Handle(ConnectionRequestAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var entity = new Notification
        {
            Title = "Connection request accepted",
            Description = "Your connection request has been accepted.",
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
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
