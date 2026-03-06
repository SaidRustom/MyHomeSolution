using FluentValidation.Results;
using MediatR;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Application.Features.Users.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler(IIdentityService identityService)
    : IRequestHandler<RemoveRoleCommand>
{
    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        if (!await identityService.UserExistsAsync(request.UserId, cancellationToken))
            throw new NotFoundException("User", request.UserId);

        var result = await identityService.RemoveFromRoleAsync(
            request.UserId, request.Role, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new ValidationFailure(string.Empty, e));

            throw new ValidationException(failures);
        }
    }
}
