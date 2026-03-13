using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.AuditLogs.Common;

namespace MyHomeSolution.Application.Features.AuditLogs.Queries.GetEntityAuditHistory;

public sealed class GetEntityAuditHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService)
    : IRequestHandler<GetEntityAuditHistoryQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(
        GetEntityAuditHistoryQuery request, CancellationToken cancellationToken)
    {
        var logs = await dbContext.AuditLogs
            .AsNoTracking()
            .Include(l => l.HistoryEntries)
            .Where(l => l.EntityName == request.EntityName && l.EntityId == request.EntityId)
            .OrderByDescending(l => l.Timestamp)
            .Take(request.MaxResults)
            .ToListAsync(cancellationToken);

        // Resolve user names
        var userIds = logs
            .Where(l => !string.IsNullOrEmpty(l.UserId))
            .Select(l => l.UserId!)
            .Distinct();

        var nameMap = await identityService.GetUserFullNamesByIdsAsync(userIds, cancellationToken);

        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            ActionType = l.ActionType,
            Timestamp = l.Timestamp,
            UserId = l.UserId,
            UserFullName = l.UserId is not null ? nameMap.GetValueOrDefault(l.UserId) : null,
            Changes = l.HistoryEntries.Select(e => new AuditHistoryEntryDto
            {
                Id = e.Id,
                PropertyName = e.PropertyName,
                OldValue = e.OldValue,
                NewValue = e.NewValue
            }).ToList()
        }).ToList();
    }
}
