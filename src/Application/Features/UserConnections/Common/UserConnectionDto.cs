using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Common;

public sealed record UserConnectionDto
{
    public Guid Id { get; init; }
    public required string RequesterId { get; init; }
    public required string AddresseeId { get; init; }
    public ConnectionStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }

    /// <summary>
    /// The user id of the other party (i.e. not the current user).
    /// Populated by query handlers for convenience.
    /// </summary>
    public string? ConnectedUserId { get; init; }
    public string? ConnectedUserName { get; init; }
    public string? ConnectedUserEmail { get; init; }
}
