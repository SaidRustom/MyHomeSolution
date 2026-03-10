using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskCreatedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Include(t => t.RecurrencePattern!)
                .ThenInclude(rp => rp.Assignees)
            .FirstOrDefaultAsync(t => t.Id == notification.TaskId, cancellationToken);

        if (task is null || string.IsNullOrEmpty(task.CreatedBy))
            return;

        // Collect all users who should be notified (assigned user + recurrence assignees)
        var recipientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(task.AssignedToUserId) && task.AssignedToUserId != task.CreatedBy)
            recipientIds.Add(task.AssignedToUserId);

        if (task.RecurrencePattern?.Assignees is { Count: > 0 })
        {
            foreach (var assignee in task.RecurrencePattern.Assignees)
            {
                if (!string.IsNullOrEmpty(assignee.UserId) && assignee.UserId != task.CreatedBy)
                    recipientIds.Add(assignee.UserId);
            }
        }

        foreach (var recipientId in recipientIds)
        {
            var entity = new Notification
            {
                Title = $"Task assigned: {task.Title}",
                Description = $"You have been assigned the task '{task.Title}'.",
                Type = NotificationType.TaskAssigned,
                FromUserId = task.CreatedBy,
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

        // Also send a realtime push to the creator so their UI refreshes
        await realtimeService.SendUserNotificationAsync(
            task.CreatedBy,
            new UserPushNotification
            {
                EventType = nameof(TaskCreatedEvent),
                Title = $"Task created: {task.Title}",
                Description = $"Your task '{task.Title}' has been created successfully.",
                RelatedEntityId = task.Id,
                RelatedEntityType = EntityTypes.HouseholdTask,
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
