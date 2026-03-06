using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceStartedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceStartedEvent>
{
    public async Task Handle(OccurrenceStartedEvent notification, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .AsNoTracking()
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == notification.OccurrenceId, cancellationToken);

        if (occurrence is null)
            return;

        var starter = notification.StartedByUserId;
        var taskOwner = occurrence.HouseholdTask.CreatedBy;

        if (string.IsNullOrEmpty(starter)
            || string.IsNullOrEmpty(taskOwner)
            || taskOwner == starter)
            return;

        var entity = new Notification
        {
            Title = "Occurrence started",
            Description = $"An occurrence of '{notification.TaskTitle}' has been started.",
            Type = NotificationType.OccurrenceStarted,
            FromUserId = starter,
            ToUserId = taskOwner,
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
