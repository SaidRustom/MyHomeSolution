using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.ToggleUserActivation;

public sealed record ToggleUserActivationCommand : IRequest
{
    public required string UserId { get; init; }
    public required bool IsActive { get; init; }
}
