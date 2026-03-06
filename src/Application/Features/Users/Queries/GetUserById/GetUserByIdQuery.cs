using MediatR;
using MyHomeSolution.Application.Features.Users.Common;

namespace MyHomeSolution.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(string UserId) : IRequest<UserDetailDto>;
