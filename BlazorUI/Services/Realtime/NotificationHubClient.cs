using BlazorUI.Infrastructure.Realtime;
using BlazorUI.Models.Realtime;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorUI.Services.Realtime;

public sealed class NotificationHubClient : INotificationHubClient
{
    private readonly HubConnectionManager _manager;
    private HubConnection? _connection;
    private readonly List<IDisposable> _subscriptions = [];
    private Func<string?, Task>? _reconnectedHandler;

    public event Action<UserPushNotification>? OnUserNotification;
    public event Action? OnReconnected;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public NotificationHubClient(HubConnectionManager manager)
    {
        _manager = manager;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        _connection = await _manager.GetOrCreateAsync(
            HubConstants.NotificationHubPath, cancellationToken);

        Subscribe();
    }

    private void Subscribe()
    {
        if (_connection is null) return;

        Unsubscribe();

        _subscriptions.Add(
            _connection.On<UserPushNotification>(
                HubConstants.Methods.UserNotification,
                notification => OnUserNotification?.Invoke(notification)));

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
            await _manager.StopAsync(HubConstants.NotificationHubPath);
            _connection = null;
        }
    }
}
