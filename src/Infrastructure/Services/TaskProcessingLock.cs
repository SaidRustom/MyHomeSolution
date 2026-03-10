using System.Collections.Concurrent;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class TaskProcessingLock : ITaskProcessingLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        Guid taskId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(taskId, _ => new SemaphoreSlim(1, 1));

        if (!await semaphore.WaitAsync(timeout, cancellationToken))
            return null;

        return new LockHandle(semaphore);
    }

    private sealed class LockHandle(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
