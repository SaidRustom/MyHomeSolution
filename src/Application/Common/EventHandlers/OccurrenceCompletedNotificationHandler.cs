using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceCompletedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceCompletedEvent>
{
    public async Task Handle(OccurrenceCompletedEvent notification, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .AsNoTracking()
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == notification.OccurrenceId, cancellationToken);

        if (occurrence is null)
            return;

        var completer = notification.CompletedByUserId;
        var taskOwner = occurrence.HouseholdTask.CreatedBy;
        var assignedUser = occurrence.AssignedToUserId;

        if (string.IsNullOrEmpty(completer))
            return;

        // Notify task owner (existing behavior)
        if (!string.IsNullOrEmpty(taskOwner) && taskOwner != completer)
        {
            await CreateAndSendNotificationAsync(
                title: "Occurrence completed",
                description: $"An occurrence of '{occurrence.HouseholdTask.Title}' has been completed.",
                type: NotificationType.OccurrenceCompleted,
                fromUserId: completer,
                toUserId: taskOwner,
                relatedEntityId: occurrence.HouseholdTaskId,
                relatedEntityType: EntityTypes.HouseholdTask,
                cancellationToken);
        }

        // Notify assigned user when completed by someone else
        if (!string.IsNullOrEmpty(assignedUser)
            && assignedUser != completer
            && assignedUser != taskOwner) // avoid double notification
        {
            await CreateAndSendNotificationAsync(
                title: "Your task was completed",
                description: $"'{occurrence.HouseholdTask.Title}' assigned to you was completed by someone else.",
                type: NotificationType.OccurrenceCompletedByOther,
                fromUserId: completer,
                toUserId: assignedUser,
                relatedEntityId: occurrence.HouseholdTaskId,
                relatedEntityType: EntityTypes.HouseholdTask,
                cancellationToken);
        }

        // Notify default payer when auto-bill was created and requires payment
        if (occurrence.HouseholdTask.AutoCreateBill && occurrence.BillId is not null)
        {
            var defaultPayer = occurrence.HouseholdTask.DefaultBillPaidByUserId;
            if (!string.IsNullOrEmpty(defaultPayer) && defaultPayer != completer)
            {
                await CreateAndSendNotificationAsync(
                    title: "Bill requires your payment",
                    description: $"A bill for '{occurrence.HouseholdTask.Title}' has been created and requires your payment.",
                    type: NotificationType.BillRequiresPayment,
                    fromUserId: completer,
                    toUserId: defaultPayer,
                    relatedEntityId: occurrence.BillId.Value,
                    relatedEntityType: EntityTypes.Bill,
                    cancellationToken);
            }

            // If no default payer, notify all split users that have unpaid splits
            if (string.IsNullOrEmpty(defaultPayer))
            {
                var bill = await dbContext.Bills
                    .AsNoTracking()
                    .Include(b => b.Splits)
                    .FirstOrDefaultAsync(b => b.Id == occurrence.BillId, cancellationToken);

                if (bill is not null)
                {
                    var unpaidUsers = bill.Splits
                        .Where(s => s.Status == SplitStatus.Unpaid && s.UserId != completer)
                        .Select(s => s.UserId)
                        .Distinct();

                    foreach (var userId in unpaidUsers)
                    {
                        await CreateAndSendNotificationAsync(
                            title: "New bill requires your attention",
                            description: $"A bill of {bill.Amount:F2} {bill.Currency} for '{occurrence.HouseholdTask.Title}' needs payment.",
                            type: NotificationType.BillRequiresPayment,
                            fromUserId: completer,
                            toUserId: userId,
                            relatedEntityId: bill.Id,
                            relatedEntityType: EntityTypes.Bill,
                            cancellationToken);
                    }
                }
            }
        }
    }

    private async Task CreateAndSendNotificationAsync(
        string title, string description, NotificationType type,
        string fromUserId, string toUserId, Guid relatedEntityId,
        string relatedEntityType,
        CancellationToken cancellationToken)
    {
        var entity = new Notification
        {
            Title = title,
            Description = description,
            Type = type,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType
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
