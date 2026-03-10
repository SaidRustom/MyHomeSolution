using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Captures a single execution cycle of a background service.
/// </summary>
public sealed class BackgroundServiceLog : BaseEntity
{
    public Guid BackgroundServiceId { get; set; }

    public BackgroundServiceDefinition BackgroundService { get; set; } = default!;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public BackgroundServiceRunStatus Status { get; set; }

    /// <summary>Human-readable result summary.</summary>
    public string? ResultMessage { get; set; }

    /// <summary>FK to ExceptionLog when the cycle failed with an exception.</summary>
    public Guid? ExceptionLogId { get; set; }

    public ExceptionLog? ExceptionLog { get; set; }
}
