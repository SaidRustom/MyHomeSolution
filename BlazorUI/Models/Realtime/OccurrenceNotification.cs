namespace BlazorUI.Models.Realtime;

public sealed record OccurrenceNotification
{
    public required string EventType { get; init; }
    public Guid OccurrenceId { get; init; }
    public Guid TaskId { get; init; }
    public string? CompletedByUserId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
