namespace MyHomeSolution.Application.Common.Interfaces;

public interface IOccurrenceScheduler
{
    Task RegenerateOccurrencesAsync(Guid taskId, CancellationToken cancellationToken = default);
}
