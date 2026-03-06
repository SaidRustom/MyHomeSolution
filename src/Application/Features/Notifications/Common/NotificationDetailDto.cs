using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Notifications.Common;

public sealed record NotificationDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public NotificationType Type { get; init; }
    public required string FromUserId { get; init; }
    public required string ToUserId { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
