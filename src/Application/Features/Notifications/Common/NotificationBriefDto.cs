using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Notifications.Common;

public sealed record NotificationBriefDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public NotificationType Type { get; init; }
    public string? FromUserId { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
