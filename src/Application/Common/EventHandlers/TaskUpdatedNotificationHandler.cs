using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskUpdatedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskUpdatedEvent>
{
    public async Task Handle(TaskUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == notification.TaskId, cancellationToken);

        if (task is null || string.IsNullOrEmpty(task.LastModifiedBy))
            return;

        var updater = task.LastModifiedBy;
        var recipients = new HashSet<string>();

        if (!string.IsNullOrEmpty(task.AssignedToUserId) && task.AssignedToUserId != updater)
            recipients.Add(task.AssignedToUserId);

        if (!string.IsNullOrEmpty(task.CreatedBy) && task.CreatedBy != updater)
            recipients.Add(task.CreatedBy);

        foreach (var recipientId in recipients)
        {
            var entity = new Notification
            {
                Title = $"Task updated: {task.Title}",
                Description = $"The task '{task.Title}' has been updated.",
                Type = NotificationType.TaskUpdated,
                FromUserId = updater,
                ToUserId = recipientId,
                RelatedEntityId = task.Id,
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
}
