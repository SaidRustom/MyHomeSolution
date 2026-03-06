using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskUpdatedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskUpdatedEvent>
{
    public Task Handle(TaskUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendTaskNotificationAsync(new TaskNotification
        {
            EventType = nameof(TaskUpdatedEvent),
            TaskId = notification.TaskId,
            Title = notification.Title,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
