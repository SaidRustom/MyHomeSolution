using MediatR;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByDateRange;

public sealed record GetOccurrencesByDateRangeQuery : IRequest<IReadOnlyCollection<CalendarOccurrenceDto>>
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string? AssignedToUserId { get; init; }
    public OccurrenceStatus? Status { get; init; }
}
