using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceRescheduledNotificationHandler(
    IApplicationDbContext dbContext,
    IRealtimeNotificationService realtimeService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceRescheduledEvent>
{
    public async Task Handle(OccurrenceRescheduledEvent notification, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .AsNoTracking()
            .Include(o => o.HouseholdTask)
            .FirstOrDefaultAsync(o => o.Id == notification.OccurrenceId, cancellationToken);

        if (occurrence is null)
            return;

        var rescheduler = notification.RescheduledByUserId;
        var assignedTo = occurrence.AssignedToUserId;

        if (string.IsNullOrEmpty(rescheduler)
            || string.IsNullOrEmpty(assignedTo)
            || assignedTo == rescheduler)
            return;

        var entity = new Notification
        {
            Title = "Occurrence rescheduled",
            Description = $"An occurrence of '{notification.TaskTitle}' was rescheduled from {notification.PreviousDate:MMM dd} to {notification.NewDate:MMM dd}.",
            Type = NotificationType.OccurrenceRescheduled,
            FromUserId = rescheduler,
            ToUserId = assignedTo,
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
