using BlazorUI.Infrastructure.Realtime;
using BlazorUI.Models.Realtime;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorUI.Services.Realtime;

public sealed class TaskHubClient : ITaskHubClient
{
    private readonly HubConnectionManager _manager;
    private HubConnection? _connection;
    private readonly List<IDisposable> _subscriptions = [];
    private Func<string?, Task>? _reconnectedHandler;

    public event Action<TaskNotification>? OnTaskNotification;
    public event Action<OccurrenceNotification>? OnOccurrenceNotification;
    public event Action? OnReconnected;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public TaskHubClient(HubConnectionManager manager)
    {
        _manager = manager;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        _connection = await _manager.GetOrCreateAsync(
            HubConstants.TaskHubPath, cancellationToken);

        Subscribe();
    }

    public async Task JoinTaskGroupAsync(
        Guid taskId, CancellationToken cancellationToken = default)
    {
        if (_connection is not null && IsConnected)
        {
            await _connection.InvokeAsync(
                HubConstants.ServerMethods.JoinTaskGroup, taskId, cancellationToken);
        }
    }

    public async Task LeaveTaskGroupAsync(
        Guid taskId, CancellationToken cancellationToken = default)
    {
        if (_connection is not null && IsConnected)
        {
            await _connection.InvokeAsync(
                HubConstants.ServerMethods.LeaveTaskGroup, taskId, cancellationToken);
        }
    }

    private void Subscribe()
    {
        if (_connection is null) return;

        Unsubscribe();

        _subscriptions.Add(
            _connection.On<TaskNotification>(
                HubConstants.Methods.TaskNotification,
                notification => OnTaskNotification?.Invoke(notification)));

        _subscriptions.Add(
            _connection.On<OccurrenceNotification>(
                HubConstants.Methods.OccurrenceNotification,
                notification => OnOccurrenceNotification?.Invoke(notification)));

        _reconnectedHandler = _ =>
        {
            OnReconnected?.Invoke();
            return Task.CompletedTask;
        };
        _connection.Reconnected += _reconnectedHandler;
    }

    private void Unsubscribe()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();

        if (_connection is not null && _reconnectedHandler is not null)
        {
            _connection.Reconnected -= _reconnectedHandler;
            _reconnectedHandler = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Unsubscribe();

        if (_connection is not null)
        {
            await _manager.StopAsync(HubConstants.TaskHubPath);
            _connection = null;
        }
    }
}
