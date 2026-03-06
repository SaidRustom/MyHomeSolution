using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<PaginatedList<UserDto>>
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
