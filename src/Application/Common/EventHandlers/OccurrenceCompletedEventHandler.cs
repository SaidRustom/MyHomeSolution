using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceCompletedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceCompletedEvent>
{
    public Task Handle(OccurrenceCompletedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendOccurrenceNotificationAsync(new OccurrenceNotification
        {
            EventType = nameof(OccurrenceCompletedEvent),
            OccurrenceId = notification.OccurrenceId,
            TaskId = notification.TaskId,
            CompletedByUserId = notification.CompletedByUserId,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
