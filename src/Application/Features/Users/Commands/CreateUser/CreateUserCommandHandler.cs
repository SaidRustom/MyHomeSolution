using FluentValidation.Results;
using MediatR;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Interfaces;
using ValidationException = MyHomeSolution.Application.Common.Exceptions.ValidationException;

namespace MyHomeSolution.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await identityService.CreateUserAsync(
            request.Email, request.Password, request.FirstName, request.LastName,
            cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(e => new ValidationFailure(string.Empty, e));

            throw new ValidationException(failures);
        }

        await identityService.AssignToRoleAsync(userId, Roles.Member, cancellationToken);

        return userId;
    }
}
