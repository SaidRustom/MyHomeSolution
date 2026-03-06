using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.RemoveRole;

public sealed record RemoveRoleCommand : IRequest
{
    public required string UserId { get; init; }
    public required string Role { get; init; }
}
