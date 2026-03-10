using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.BackgroundServices.Common;

namespace MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServiceLogs;

public sealed class GetBackgroundServiceLogsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetBackgroundServiceLogsQuery, PaginatedList<BackgroundServiceLogBriefDto>>
{
    public async Task<PaginatedList<BackgroundServiceLogBriefDto>> Handle(
        GetBackgroundServiceLogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.BackgroundServiceLogs
            .AsNoTracking()
            .Where(l => l.BackgroundServiceId == request.BackgroundServiceId)
            .OrderByDescending(l => l.StartedAt)
            .Select(l => new BackgroundServiceLogBriefDto
            {
                Id = l.Id,
                BackgroundServiceId = l.BackgroundServiceId,
                StartedAt = l.StartedAt,
                CompletedAt = l.CompletedAt,
                Status = l.Status.ToString(),
                ResultMessage = l.ResultMessage,
                ExceptionLogId = l.ExceptionLogId
            });

        return await PaginatedList<BackgroundServiceLogBriefDto>.CreateAsync(
            query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
