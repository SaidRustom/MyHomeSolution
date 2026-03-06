using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ShoppingListCreatedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ShoppingListCreatedEvent>
{
    public async Task Handle(ShoppingListCreatedEvent notification, CancellationToken cancellationToken)
    {
        var sharedUserIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == notification.ShoppingListId
                && !s.IsDeleted
                && s.SharedWithUserId != notification.CreatedByUserId)
            .Select(s => s.SharedWithUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var recipientUserId in sharedUserIds)
        {
            var entity = new Notification
            {
                Title = $"New shopping list: {notification.Title}",
                Description = $"A new shopping list '{notification.Title}' has been shared with you.",
                Type = NotificationType.ShoppingListCreated,
                FromUserId = notification.CreatedByUserId,
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
