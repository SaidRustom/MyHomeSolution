using MediatR;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Shares.Commands.ShareEntity;

public sealed record ShareEntityCommand : IRequest<Guid>
{
    public required string EntityType { get; init; }
    public Guid EntityId { get; init; }
    public required string SharedWithUserId { get; init; }
    public SharePermission Permission { get; init; }
}
