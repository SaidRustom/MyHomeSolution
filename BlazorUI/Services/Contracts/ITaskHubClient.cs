using BlazorUI.Models.Realtime;

namespace BlazorUI.Services.Contracts;

public interface ITaskHubClient : IAsyncDisposable
{
    event Action<TaskNotification>? OnTaskNotification;
    event Action<OccurrenceNotification>? OnOccurrenceNotification;
    event Action? OnReconnected;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task JoinTaskGroupAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task LeaveTaskGroupAsync(Guid taskId, CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}
