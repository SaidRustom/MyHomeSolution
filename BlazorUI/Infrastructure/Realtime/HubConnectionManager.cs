using BlazorUI.Infrastructure.Auth;
using BlazorUI.Infrastructure.LocalStorage;
using BlazorUI.Models.Auth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace BlazorUI.Infrastructure.Realtime;

public sealed class HubConnectionManager : IAsyncDisposable
{
    private readonly IStorageManager _storage;
    private readonly ILogger<HubConnectionManager> _logger;
    private readonly string _baseUrl;
    private readonly Dictionary<string, HubConnection> _connections = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public HubConnectionManager(
        IStorageManager storage,
        IConfiguration configuration,
        ILogger<HubConnectionManager> logger)
    {
        _storage = storage;
        _logger = logger;
        _baseUrl = configuration.GetSection("Api")["BaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    }

    public async Task<HubConnection> GetOrCreateAsync(
        string hubPath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connections.TryGetValue(hubPath, out var existing)
                && existing.State != HubConnectionState.Disconnected)
            {
                return existing;
            }

            if (existing is not null)
            {
                await DisposeConnectionAsync(hubPath, existing);
            }

            var connection = BuildConnection(hubPath);

            connection.Closed += ex =>
            {
                if (ex is not null)
                {
                    _logger.LogWarning(ex, "Hub connection to {HubPath} closed with error", hubPath);
                }
                else
                {
                    _logger.LogInformation("Hub connection to {HubPath} closed gracefully", hubPath);
                }
                return Task.CompletedTask;
            };

            connection.Reconnecting += ex =>
            {
                _logger.LogInformation(ex, "Reconnecting to hub {HubPath}", hubPath);
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                _logger.LogInformation(
                    "Reconnected to hub {HubPath} with ConnectionId {ConnectionId}",
                    hubPath, connectionId);
                return Task.CompletedTask;
            };

            await connection.StartAsync(cancellationToken);
            _connections[hubPath] = connection;

            _logger.LogInformation(
                "SignalR connection established to {HubPath} (State={State})",
                hubPath, connection.State);

            return connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public HubConnectionState GetState(string hubPath)
    {
        return _connections.TryGetValue(hubPath, out var connection)
            ? connection.State
            : HubConnectionState.Disconnected;
    }

    public async Task StopAsync(string hubPath, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connections.TryGetValue(hubPath, out var connection))
            {
                await DisposeConnectionAsync(hubPath, connection);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private HubConnection BuildConnection(string hubPath)
    {
        var url = $"{_baseUrl}{hubPath}";

        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = GetAccessTokenAsync;
            })
            .WithAutomaticReconnect(new RetryPolicy())
            .Build();
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        var tokens = await _storage.GetAsync<AuthTokens>(AuthConstants.TokenStorageKey);
        if (tokens is null || tokens.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        return tokens.AccessToken;
    }

    private async Task DisposeConnectionAsync(string hubPath, HubConnection connection)
    {
        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing hub connection to {HubPath}", hubPath);
        }

        _connections.Remove(hubPath);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _lock.WaitAsync();
        try
        {
            foreach (var (path, connection) in _connections)
            {
                try
                {
                    await connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing hub connection to {HubPath} during shutdown", path);
                }
            }

            _connections.Clear();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }

    private sealed class RetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan[] Delays =
        [
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60)
        ];

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var index = Math.Min(retryContext.PreviousRetryCount, Delays.Length - 1);
            return Delays[index];
        }
    }
}
