using MediatR;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    public Task<PaginatedList<UserDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        return identityService.GetUsersAsync(
            request.SearchTerm,
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
