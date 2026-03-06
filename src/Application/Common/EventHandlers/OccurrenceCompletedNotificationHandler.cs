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

        if (string.IsNullOrEmpty(completer)
            || string.IsNullOrEmpty(taskOwner)
            || taskOwner == completer)
            return;

        var entity = new Notification
        {
            Title = "Occurrence completed",
            Description = $"An occurrence of '{occurrence.HouseholdTask.Title}' has been completed.",
            Type = NotificationType.OccurrenceCompleted,
            FromUserId = completer,
            ToUserId = taskOwner,
            RelatedEntityId = occurrence.HouseholdTaskId,
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
