namespace BlazorUI.Models.BackgroundServices;

public sealed record BackgroundServiceLogBriefDto
{
    public Guid Id { get; init; }
    public Guid BackgroundServiceId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = default!;
    public string? ResultMessage { get; init; }
    public Guid? ExceptionLogId { get; init; }

    public string StatusColor => Status switch
    {
        "Completed" => "var(--rz-success)",
        "Failed" => "var(--rz-danger)",
        "Running" => "var(--rz-info)",
        _ => "var(--rz-secondary)"
    };

    public string StatusIcon => Status switch
    {
        "Completed" => "check_circle",
        "Failed" => "error",
        "Running" => "sync",
        _ => "help_outline"
    };

    public string DurationDisplay
    {
        get
        {
            if (!CompletedAt.HasValue) return "Running…";
            var duration = CompletedAt.Value - StartedAt;
            if (duration.TotalSeconds < 1) return $"{duration.TotalMilliseconds:F0}ms";
            if (duration.TotalMinutes < 1) return $"{duration.TotalSeconds:F1}s";
            return $"{duration.TotalMinutes:F1}m";
        }
    }
}
