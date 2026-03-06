using MediatR;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class BillDeletedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<BillDeletedEvent>
{
    public async Task Handle(BillDeletedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var recipientUserId in notification.AffectedUserIds)
        {
            var entity = new Notification
            {
                Title = $"Bill deleted: {notification.Title}",
                Description = $"The bill '{notification.Title}' has been deleted.",
                Type = NotificationType.BillDeleted,
                FromUserId = notification.DeletedByUserId,
                ToUserId = recipientUserId,
                RelatedEntityId = notification.BillId,
                RelatedEntityType = EntityTypes.Bill
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
}
