
using Microsoft.JSInterop;
using System.Text.Json;

namespace BlazorUI.Infrastructure.LocalStorage
{
    public class StorageManager : IStorageManager
    {
        private readonly IJSRuntime _js;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private readonly Dictionary<string, object> _memoryCache = new();

        public event EventHandler<StorageChangedEventArgs>? StorageChanged;

        public StorageManager(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            await _lock.WaitAsync();

            try
            {
                var envelope = new StorageEnvelope<T>
                {
                    Value = value,
                    ExpireAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
                };

                var json = JsonSerializer.Serialize(envelope);

                _memoryCache[key] = value!;

                await _js.InvokeVoidAsync("localStorage.setItem", key, json);

                StorageChanged?.Invoke(this, new StorageChangedEventArgs
                {
                    Key = key
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            // Memory fast path
            if (_memoryCache.TryGetValue(key, out var cached))
                return (T)cached;

            var json = await _js.InvokeAsync<string>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                var envelope = JsonSerializer.Deserialize<StorageEnvelope<T>>(json);

                if (envelope == null)
                    return default;

                if (envelope.ExpireAt.HasValue &&
                    envelope.ExpireAt.Value < DateTime.UtcNow)
                {
                    await RemoveAsync(key);
                    return default;
                }

                _memoryCache[key] = envelope.Value!;
                return envelope.Value;
            }
            catch
            {
                await RemoveAsync(key);
                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            await _lock.WaitAsync();

            try
            {
                _memoryCache.Remove(key);
                await _js.InvokeVoidAsync("localStorage.removeItem", key);

                StorageChanged?.Invoke(this, new StorageChangedEventArgs
                {
                    Key = key
                });
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
