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
            .FirstOrDefaultAsync(t => t.Id == notification.TaskId, cancellationToken);

        if (task is null)
            return;

        if (string.IsNullOrEmpty(task.AssignedToUserId)
            || string.IsNullOrEmpty(task.CreatedBy)
            || task.AssignedToUserId == task.CreatedBy)
            return;

        var entity = new Notification
        {
            Title = $"Task assigned: {task.Title}",
            Description = $"You have been assigned the task '{task.Title}'.",
            Type = NotificationType.TaskAssigned,
            FromUserId = task.CreatedBy,
            ToUserId = task.AssignedToUserId,
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
