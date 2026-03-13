using MediatR;
using MyHomeSolution.Application.Features.AuditLogs.Common;

namespace MyHomeSolution.Application.Features.AuditLogs.Queries.GetEntityAuditHistory;

public sealed record GetEntityAuditHistoryQuery : IRequest<IReadOnlyList<AuditLogDto>>
{
    public required string EntityName { get; init; }
    public required string EntityId { get; init; }
    public int MaxResults { get; init; } = 50;
}
