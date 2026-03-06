using MediatR;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.SearchConnectedUsers;

/// <summary>
/// Searches the current user's accepted connections by name or email.
/// Used to power the friend-picker autocomplete component.
/// </summary>
public sealed record SearchConnectedUsersQuery : IRequest<IReadOnlyList<UserDto>>
{
    public string? SearchTerm { get; init; }
    public int MaxResults { get; init; } = 20;
}
