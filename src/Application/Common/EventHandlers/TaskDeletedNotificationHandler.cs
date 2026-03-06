using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskDeletedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskDeletedEvent>
{
    public async Task Handle(TaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == notification.TaskId, cancellationToken);

        if (task is null)
            return;

        var deleter = task.DeletedBy ?? task.LastModifiedBy;

        if (string.IsNullOrEmpty(deleter)
            || string.IsNullOrEmpty(task.AssignedToUserId)
            || task.AssignedToUserId == deleter)
            return;

        var entity = new Notification
        {
            Title = $"Task deleted: {task.Title}",
            Description = $"The task '{task.Title}' has been deleted.",
            Type = NotificationType.TaskDeleted,
            FromUserId = deleter,
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
                OccurredAt = dateTimeProvider.UtcNow
            },
            cancellationToken);
    }
}
