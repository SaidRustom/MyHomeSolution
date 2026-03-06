using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Notifications;

public sealed record NotificationBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public NotificationType Type { get; init; }
    public string? FromUserId { get; init; }
    public string? FromUserFullName { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
