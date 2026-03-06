using FluentValidation.Results;
using MediatR;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Application.Features.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!await identityService.UserExistsAsync(request.UserId, cancellationToken))
            throw new NotFoundException("User", request.UserId);

        var result = await identityService.ChangePasswordAsync(
            request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new ValidationFailure(string.Empty, e));

            throw new ValidationException(failures);
        }
    }
}
