using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskCreatedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskCreatedEvent>
{
    public Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendTaskNotificationAsync(new TaskNotification
        {
            EventType = nameof(TaskCreatedEvent),
            TaskId = notification.TaskId,
            Title = notification.Title,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
