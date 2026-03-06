using MediatR;
using MyHomeSolution.Application.Features.Shares.Common;

namespace MyHomeSolution.Application.Features.Shares.Queries.GetEntityShares;

public sealed record GetEntitySharesQuery : IRequest<IReadOnlyList<ShareDto>>
{
    public required string EntityType { get; init; }
    public Guid EntityId { get; init; }
}
