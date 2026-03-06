using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceSkippedNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceSkippedEvent>
{
    public async Task Handle(OccurrenceSkippedEvent notification, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .AsNoTracking()
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == notification.OccurrenceId, cancellationToken);

        if (occurrence is null)
            return;

        var skipper = occurrence.LastModifiedBy;
        var taskOwner = occurrence.HouseholdTask.CreatedBy;

        if (string.IsNullOrEmpty(skipper)
            || string.IsNullOrEmpty(taskOwner)
            || taskOwner == skipper)
            return;

        var entity = new Notification
        {
            Title = "Occurrence skipped",
            Description = $"An occurrence of '{occurrence.HouseholdTask.Title}' has been skipped.",
            Type = NotificationType.OccurrenceSkipped,
            FromUserId = skipper,
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
