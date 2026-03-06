using MediatR;

namespace MyHomeSolution.Application.Features.Users.Commands.ChangePassword;

public sealed record ChangePasswordCommand : IRequest
{
    public required string UserId { get; init; }
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}
