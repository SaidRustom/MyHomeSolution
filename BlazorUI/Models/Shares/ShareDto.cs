using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Shares;

public sealed record ShareDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public required string EntityType { get; init; }
    public required string SharedWithUserId { get; init; }
    public SharePermission Permission { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
