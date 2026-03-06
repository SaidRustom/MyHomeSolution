using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand : IRequest<string>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
