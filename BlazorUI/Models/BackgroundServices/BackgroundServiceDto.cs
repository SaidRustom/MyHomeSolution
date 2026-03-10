namespace BlazorUI.Models.BackgroundServices;

public sealed record BackgroundServiceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string QualifiedTypeName { get; init; } = default!;
    public bool IsEnabled { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
    public BackgroundServiceLogBriefDto? LatestLog { get; init; }
}
