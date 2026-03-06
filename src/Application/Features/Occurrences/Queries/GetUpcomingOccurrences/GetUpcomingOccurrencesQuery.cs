using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetUpcomingOccurrences;

public sealed record GetUpcomingOccurrencesQuery : IRequest<PaginatedList<CalendarOccurrenceDto>>
{
    public int Count { get; init; } = 10;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
