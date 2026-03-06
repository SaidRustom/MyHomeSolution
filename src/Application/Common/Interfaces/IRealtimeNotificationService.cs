using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IRealtimeNotificationService
{
    Task SendTaskNotificationAsync(
        TaskNotification notification, CancellationToken cancellationToken = default);

    Task SendOccurrenceNotificationAsync(
        OccurrenceNotification notification, CancellationToken cancellationToken = default);

    Task SendUserNotificationAsync(
        string userId, UserPushNotification notification, CancellationToken cancellationToken = default);
}
