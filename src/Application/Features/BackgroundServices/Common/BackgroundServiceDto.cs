namespace MyHomeSolution.Application.Features.BackgroundServices.Common;

public sealed record BackgroundServiceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string QualifiedTypeName { get; init; } = default!;
    public bool IsEnabled { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }

    /// <summary>The latest log entry for quick status display.</summary>
    public BackgroundServiceLogBriefDto? LatestLog { get; init; }
}
