using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand : IRequest
{
    public required string UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? AvatarUrl { get; init; }
}
