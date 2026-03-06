using BlazorUI.Models.Enums;

namespace BlazorUI.Models.UserConnections;

public sealed record UserConnectionDto
{
    public Guid Id { get; init; }
    public required string RequesterId { get; init; }
    public required string AddresseeId { get; init; }
    public ConnectionStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public string? ConnectedUserId { get; init; }
    public string? ConnectedUserName { get; init; }
    public string? ConnectedUserEmail { get; init; }
}
