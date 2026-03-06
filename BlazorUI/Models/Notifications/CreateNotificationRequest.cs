using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Notifications;

public sealed record CreateNotificationRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public NotificationType Type { get; init; }
    public required string ToUserId { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
}
