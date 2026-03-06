using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByTask;

public sealed record GetOccurrencesByTaskQuery : IRequest<PaginatedList<OccurrenceDto>>
{
    public Guid HouseholdTaskId { get; init; }
    public OccurrenceStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
