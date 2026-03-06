using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class NotificationCreatedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<NotificationCreatedEvent>
{
    public Task Handle(NotificationCreatedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendUserNotificationAsync(
            notification.ToUserId,
            new UserPushNotification
            {
                EventType = nameof(NotificationCreatedEvent),
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Description = notification.Description,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
