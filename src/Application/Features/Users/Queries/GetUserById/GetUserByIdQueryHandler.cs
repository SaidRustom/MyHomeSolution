using MediatR;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    public async Task<UserDetailDto> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserByIdAsync(request.UserId, cancellationToken);

        return user ?? throw new NotFoundException("User", request.UserId);
    }
}
