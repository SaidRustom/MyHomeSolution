using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class BillReceiptAddedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<BillReceiptAddedEvent>
{
    public async Task Handle(BillReceiptAddedEvent notification, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == notification.BillId, cancellationToken);

        if (bill is null)
            return;

        var recipientUserIds = bill.Splits
            .Where(s => s.UserId != notification.AddedByUserId)
            .Select(s => s.UserId)
            .Distinct();

        foreach (var recipientUserId in recipientUserIds)
        {
            var entity = new Notification
            {
                Title = $"Receipt added: {bill.Title}",
                Description = $"A receipt photo has been added to the bill '{bill.Title}'.",
                Type = NotificationType.BillReceiptAdded,
                FromUserId = notification.AddedByUserId,
                ToUserId = recipientUserId,
                RelatedEntityId = bill.Id,
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
