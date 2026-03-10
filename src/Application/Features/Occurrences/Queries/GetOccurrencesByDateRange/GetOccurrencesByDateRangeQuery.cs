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
    public bool? AssignedByMe { get; init; }
    public bool? MyTasks { get; init; }
    public bool? Private { get; init; }
    public bool? Shared { get; init; }
    public bool? IsRecurring { get; init; }
    public bool? HasBill { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskPriority? Priority { get; init; }
}
