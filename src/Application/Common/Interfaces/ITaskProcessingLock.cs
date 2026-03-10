namespace MyHomeSolution.Application.Common.Interfaces;

public interface ITaskProcessingLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(Guid taskId, TimeSpan timeout, CancellationToken cancellationToken = default);
}
