using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class BillSplitPaidNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<BillSplitPaidEvent>
{
    public async Task Handle(BillSplitPaidEvent notification, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == notification.BillId, cancellationToken);

        if (bill is null)
            return;

        if (bill.PaidByUserId == notification.PaidByUserId)
            return;

        var entity = new Notification
        {
            Title = $"Payment received: {bill.Title}",
            Description = $"{notification.PaidByUserId} marked their share of {notification.Amount:C} as paid for '{bill.Title}'.",
            Type = NotificationType.BillSplitPaid,
            FromUserId = notification.PaidByUserId,
            ToUserId = bill.PaidByUserId,
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
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
