using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ShareRevokedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ShareRevokedEvent>
{
    public async Task Handle(ShareRevokedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.SharedWithUserId == notification.RevokedByUserId)
            return;

        var entity = new Notification
        {
            Title = "Share access revoked",
            Description = $"Your access to a {notification.EntityType} has been revoked.",
            Type = NotificationType.ShareRevoked,
            FromUserId = notification.RevokedByUserId,
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
                Description = entity.Description,
                RelatedEntityId = entity.RelatedEntityId,
                RelatedEntityType = entity.RelatedEntityType,
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
