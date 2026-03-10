namespace MyHomeSolution.Application.Common.Interfaces;

public interface IOccurrenceScheduler
{
    /// <summary>
    /// Reconciles existing occurrences with the current recurrence pattern
    /// using a diff-based approach: completed/in-progress/overdue occurrences
    /// are preserved, pending occurrences are re-aligned, extras are removed,
    /// and missing dates are filled.
    /// </summary>
    Task SyncOccurrencesAsync(Guid taskId, CancellationToken cancellationToken = default);
}
