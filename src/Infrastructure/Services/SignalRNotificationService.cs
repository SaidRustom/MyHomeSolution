using Microsoft.AspNetCore.SignalR;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Infrastructure.Hubs;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class SignalRNotificationService(
    IHubContext<TaskHub> taskHubContext,
    IHubContext<NotificationHub> notificationHubContext)
    : IRealtimeNotificationService
{
    public const string TaskNotificationMethod = "TaskNotification";
    public const string OccurrenceNotificationMethod = "OccurrenceNotification";
    public const string UserNotificationMethod = "UserNotification";

    public Task SendTaskNotificationAsync(
        TaskNotification notification, CancellationToken cancellationToken)
    {
        return taskHubContext.Clients.All
            .SendAsync(TaskNotificationMethod, notification, cancellationToken);
    }

    public Task SendOccurrenceNotificationAsync(
        OccurrenceNotification notification, CancellationToken cancellationToken)
    {
        var groupName = TaskHub.FormatGroupName(notification.TaskId);

        return taskHubContext.Clients.Group(groupName)
            .SendAsync(OccurrenceNotificationMethod, notification, cancellationToken);
    }

    public Task SendUserNotificationAsync(
        string userId, UserPushNotification notification, CancellationToken cancellationToken)
    {
        var groupName = NotificationHub.FormatGroupName(userId);

        return notificationHubContext.Clients.Group(groupName)
            .SendAsync(UserNotificationMethod, notification, cancellationToken);
    }
}
