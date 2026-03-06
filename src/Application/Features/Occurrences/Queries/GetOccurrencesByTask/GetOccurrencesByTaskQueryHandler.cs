using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;

namespace MyHomeSolution.Application.Features.Occurrences.Queries.GetOccurrencesByTask;

public sealed class GetOccurrencesByTaskQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetOccurrencesByTaskQuery, PaginatedList<OccurrenceDto>>
{
    public async Task<PaginatedList<OccurrenceDto>> Handle(
        GetOccurrencesByTaskQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.TaskOccurrences
            .AsNoTracking()
            .Where(o => o.HouseholdTaskId == request.HouseholdTaskId && !o.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var projected = query
            .OrderBy(o => o.DueDate)
            .Select(o => new OccurrenceDto
            {
                Id = o.Id,
                DueDate = o.DueDate,
                Status = o.Status,
                AssignedToUserId = o.AssignedToUserId,
                CompletedAt = o.CompletedAt,
                Notes = o.Notes
            });

        return await PaginatedList<OccurrenceDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
