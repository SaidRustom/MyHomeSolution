using FluentValidation.Results;
using MediatR;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (!await identityService.UserExistsAsync(request.UserId, cancellationToken))
            throw new NotFoundException("User", request.UserId);

        var result = await identityService.UpdateUserAsync(
            request.UserId, request.FirstName, request.LastName,
            request.Email, request.AvatarUrl, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new ValidationFailure(string.Empty, e));

            throw new ValidationException(failures);
        }
    }
}
