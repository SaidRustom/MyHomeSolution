using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class AvatarService(HttpClient httpClient) : ApiServiceBase(httpClient), IAvatarService
{
    private const string BasePath = "api/users";

    private readonly Dictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<string?>> _inflight = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string?> GetAvatarDataUrlAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        if (_cache.TryGetValue(userId, out var cached))
            return cached;

        // Deduplicate concurrent requests for the same user
        Task<string?>? existing;
        lock (_inflight)
        {
            if (!_inflight.TryGetValue(userId, out existing))
            {
                existing = FetchAvatarAsync(userId, cancellationToken);
                _inflight[userId] = existing;
            }
        }

        var result = await existing;

        lock (_inflight)
        {
            _inflight.Remove(userId);
        }

        _cache[userId] = result;
        return result;
    }

    public void InvalidateCache(string userId)
    {
        _cache.Remove(userId);
    }

    private async Task<string?> FetchAvatarAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(
                $"{BasePath}/{Uri.EscapeDataString(userId)}/avatar", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (bytes.Length == 0)
                return null;

            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }
}
