using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class ShoppingItemCheckedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ShoppingItemCheckedEvent>
{
    public async Task Handle(ShoppingItemCheckedEvent notification, CancellationToken cancellationToken)
    {
        var shoppingList = await dbContext.ShoppingLists
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.Id == notification.ShoppingListId, cancellationToken);

        if (shoppingList is null)
            return;

        var sharedUserIds = await dbContext.EntityShares
            .AsNoTracking()
            .Where(s => s.EntityType == EntityTypes.ShoppingList
                && s.EntityId == notification.ShoppingListId
                && !s.IsDeleted
                && s.SharedWithUserId != notification.CheckedByUserId)
            .Select(s => s.SharedWithUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allRecipients = sharedUserIds.ToList();
        if (shoppingList.CreatedBy is not null
            && shoppingList.CreatedBy != notification.CheckedByUserId
            && !allRecipients.Contains(shoppingList.CreatedBy))
        {
            allRecipients.Add(shoppingList.CreatedBy);
        }

        var notifications = new List<Notification>();

        foreach (var recipientUserId in allRecipients)
        {
            var entity = new Notification
            {
                Title = $"Item checked: {notification.ItemName}",
                Description = $"'{notification.ItemName}' was checked off the shopping list '{notification.ShoppingListTitle}'.",
                Type = NotificationType.ShoppingItemChecked,
                FromUserId = notification.CheckedByUserId,
                ToUserId = recipientUserId,
                RelatedEntityId = notification.ShoppingListId,
                RelatedEntityType = EntityTypes.ShoppingList
            };

            dbContext.Notifications.Add(entity);
            notifications.Add(entity);
        }

        if (notifications.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var entity in notifications)
        {
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
}
