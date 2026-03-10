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

        if (string.IsNullOrEmpty(deleter))
            return;

        // Build description with cascade info
        var description = $"The task '{notification.Title}' has been deleted.";
        if (notification.DeletedBillCount > 0)
        {
            description += $" {notification.DeletedOccurrenceCount} future occurrence(s) and {notification.DeletedBillCount} unpaid bill(s) were also removed.";
        }

        // Collect all users who should be notified
        var usersToNotify = new HashSet<string>(notification.AffectedUserIds);
        if (!string.IsNullOrEmpty(task.AssignedToUserId))
            usersToNotify.Add(task.AssignedToUserId);
        usersToNotify.Remove(deleter);

        foreach (var userId in usersToNotify)
        {
            var entity = new Notification
            {
                Title = $"Task deleted: {notification.Title}",
                Description = description,
                Type = NotificationType.TaskDeleted,
                FromUserId = deleter,
                ToUserId = userId,
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
