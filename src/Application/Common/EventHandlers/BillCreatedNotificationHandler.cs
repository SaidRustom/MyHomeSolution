using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class BillCreatedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<BillCreatedEvent>
{
    public async Task Handle(BillCreatedEvent notification, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == notification.BillId, cancellationToken);

        if (bill is null)
            return;

        var recipientUserIds = bill.Splits
            .Where(s => s.UserId != notification.PaidByUserId)
            .Select(s => s.UserId)
            .Distinct();

        var notifications = new List<Notification>();

        foreach (var recipientUserId in recipientUserIds)
        {
            var split = bill.Splits.First(s => s.UserId == recipientUserId);

            var entity = new Notification
            {
                Title = $"New bill: {bill.Title}",
                Description = $"A bill of {bill.Amount:F2} {bill.Currency} has been added. Your share is {split.Amount:F2} {bill.Currency} ({split.Percentage:F1}%).",
                Type = NotificationType.BillCreated,
                FromUserId = notification.PaidByUserId,
                ToUserId = recipientUserId,
                RelatedEntityId = bill.Id,
                RelatedEntityType = EntityTypes.Bill
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
