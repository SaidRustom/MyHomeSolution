namespace MyHomeSolution.Application.Features.BackgroundServices.Common;

public sealed record BackgroundServiceLogBriefDto
{
    public Guid Id { get; init; }
    public Guid BackgroundServiceId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = default!;
    public string? ResultMessage { get; init; }
    public Guid? ExceptionLogId { get; init; }
}
