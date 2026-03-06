using BlazorUI.Models.Realtime;

namespace BlazorUI.Services.Contracts;

public interface INotificationHubClient : IAsyncDisposable
{
    event Action<UserPushNotification>? OnUserNotification;
    event Action? OnReconnected;

    Task StartAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}
