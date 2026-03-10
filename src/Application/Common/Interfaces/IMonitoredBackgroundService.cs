namespace MyHomeSolution.Application.Common.Interfaces;

/// <summary>
/// Contract for background services that participate in centralised monitoring and logging.
/// Implement this interface on any <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>
/// to automatically track execution cycles in the <c>BackgroundServiceLogs</c> table.
/// </summary>
public interface IMonitoredBackgroundService
{
    /// <summary>
    /// Deterministic identifier for the service definition row.
    /// Must be the same across restarts so seed data and logs are correlated.
    /// </summary>
    static abstract Guid ServiceId { get; }

    /// <summary>Human-readable service name (displayed in the admin UI).</summary>
    static abstract string ServiceName { get; }

    /// <summary>Short description of what the service does.</summary>
    static abstract string ServiceDescription { get; }
}
