using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ShoppingListUpdatedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ShoppingListUpdatedEvent>
{
    public async Task Handle(ShoppingListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var sharedUserIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == notification.ShoppingListId
                && !s.IsDeleted
                && s.SharedWithUserId != notification.UpdatedByUserId)
            .Select(s => s.SharedWithUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var shoppingList = await dbContext.ShoppingLists
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.Id == notification.ShoppingListId, cancellationToken);

        if (shoppingList is null)
            return;

        var owner = shoppingList.CreatedBy;
        var allRecipients = sharedUserIds.ToList();
        if (owner is not null && owner != notification.UpdatedByUserId && !allRecipients.Contains(owner))
            allRecipients.Add(owner);

        foreach (var recipientUserId in allRecipients)
        {
            var entity = new Notification
            {
                Title = $"Shopping list updated: {notification.Title}",
                Description = $"The shopping list '{notification.Title}' has been updated.",
                Type = NotificationType.ShoppingListUpdated,
                FromUserId = notification.UpdatedByUserId,
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
