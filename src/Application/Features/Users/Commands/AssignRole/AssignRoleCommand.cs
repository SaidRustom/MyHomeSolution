using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.AssignRole;

public sealed record AssignRoleCommand : IRequest
{
    public required string UserId { get; init; }
    public required string Role { get; init; }
}
