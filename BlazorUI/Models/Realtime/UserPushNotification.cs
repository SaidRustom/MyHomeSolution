namespace BlazorUI.Models.Realtime;

public sealed record UserPushNotification
{
    public required string EventType { get; init; }
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
