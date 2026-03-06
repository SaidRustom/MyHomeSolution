using MediatR;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceOverdueNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceOverdueEvent>
{
    public async Task Handle(OccurrenceOverdueEvent notification, CancellationToken cancellationToken)
    {
        var targetUserId = notification.AssignedToUserId;
        if (string.IsNullOrEmpty(targetUserId))
            return;

        var entity = new Notification
        {
            Title = "Task overdue",
            Description = $"An occurrence of '{notification.TaskTitle}' is overdue.",
            Type = NotificationType.OccurrenceOverdue,
            FromUserId = "system",
            ToUserId = targetUserId,
            RelatedEntityId = notification.TaskId,
            RelatedEntityType = EntityTypes.HouseholdTask
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
