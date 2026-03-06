using BlazorUI.Models.Enums;

namespace BlazorUI.Models.Shares;

public sealed record ShareEntityRequest
{
    public required string EntityType { get; init; }
    public Guid EntityId { get; init; }
    public required string SharedWithUserId { get; init; }
    public SharePermission Permission { get; init; }
}

public sealed record UpdateSharePermissionRequest
{
    public SharePermission Permission { get; init; }
}
