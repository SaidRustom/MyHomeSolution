using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.UserConnections.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.UserConnections.Queries.GetConnections;

public sealed record GetConnectionsQuery : IRequest<PaginatedList<UserConnectionDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ConnectionStatus? Status { get; init; }
    public string? SearchTerm { get; init; }
}
