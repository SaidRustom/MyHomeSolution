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
        var detail = notification.DeletedBillCount > 0
            ? $"Task '{notification.Title}' deleted along with {notification.DeletedOccurrenceCount} occurrence(s) and {notification.DeletedBillCount} unpaid bill(s)."
            : null;

        return notificationService.SendTaskNotificationAsync(new TaskNotification
        {
            EventType = nameof(TaskDeletedEvent),
            TaskId = notification.TaskId,
            Title = detail ?? notification.Title,
            OccurredAt = dateTimeProvider.UtcNow
        }, cancellationToken);
    }
}
