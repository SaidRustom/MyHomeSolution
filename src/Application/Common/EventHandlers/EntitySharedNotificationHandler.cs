using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class EntitySharedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<EntitySharedEvent>
{
    public async Task Handle(EntitySharedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.SharedWithUserId == notification.SharedByUserId)
            return;

        var entity = new Notification
        {
            Title = $"{notification.EntityType} shared with you",
            Description = $"A {notification.EntityType} has been shared with you.",
            Type = NotificationType.ShareReceived,
            FromUserId = notification.SharedByUserId,
            ToUserId = notification.SharedWithUserId,
            RelatedEntityId = notification.EntityId,
            RelatedEntityType = notification.EntityType
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
