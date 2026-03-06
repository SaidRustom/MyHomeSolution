using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class OccurrenceSkippedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<OccurrenceSkippedEvent>
{
    public Task Handle(OccurrenceSkippedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendOccurrenceNotificationAsync(new OccurrenceNotification
        {
            EventType = nameof(OccurrenceSkippedEvent),
            OccurrenceId = notification.OccurrenceId,
            TaskId = notification.TaskId,
            CompletedByUserId = null,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
