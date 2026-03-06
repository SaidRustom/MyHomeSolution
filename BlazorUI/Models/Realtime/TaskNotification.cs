namespace BlazorUI.Models.Realtime;

public sealed record TaskNotification
{
    public required string EventType { get; init; }
    public Guid TaskId { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
