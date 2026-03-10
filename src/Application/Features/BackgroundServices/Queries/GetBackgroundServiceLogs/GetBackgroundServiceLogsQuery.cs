using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.BackgroundServices.Common;

namespace MyHomeSolution.Application.Features.BackgroundServices.Queries.GetBackgroundServiceLogs;

public sealed record GetBackgroundServiceLogsQuery : IRequest<PaginatedList<BackgroundServiceLogBriefDto>>
{
    public Guid BackgroundServiceId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
