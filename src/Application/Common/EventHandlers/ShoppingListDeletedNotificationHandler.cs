using MediatR;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ShoppingListDeletedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ShoppingListDeletedEvent>
{
    public async Task Handle(ShoppingListDeletedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var recipientUserId in notification.SharedWithUserIds)
        {
            var entity = new Notification
            {
                Title = $"Shopping list deleted: {notification.Title}",
                Description = $"The shopping list '{notification.Title}' has been deleted.",
                Type = NotificationType.ShoppingListDeleted,
                FromUserId = notification.DeletedByUserId,
                ToUserId = recipientUserId,
                RelatedEntityId = notification.ShoppingListId,
                RelatedEntityType = EntityTypes.ShoppingList
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
