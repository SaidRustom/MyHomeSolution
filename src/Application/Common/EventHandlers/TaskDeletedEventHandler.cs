using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.EventHandlers;

public sealed class TaskDeletedEventHandler(
    IRealtimeNotificationService notificationService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<TaskDeletedEvent>
{
    public Task Handle(TaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        return notificationService.SendTaskNotificationAsync(new TaskNotification
        {
            EventType = nameof(TaskDeletedEvent),
            TaskId = notification.TaskId,
            Title = null,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
