using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTaskByIdQuery, TaskDetailDto>
{
    public async Task<TaskDetailDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Include(t => t.RecurrencePattern)
                .ThenInclude(rp => rp!.Assignees)
            .Include(t => t.Occurrences.OrderBy(o => o.DueDate))
            .Where(t => !t.IsDeleted)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdTask), request.Id);

        return new TaskDetailDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Category = task.Category,
            EstimatedDurationMinutes = task.EstimatedDurationMinutes,
            IsRecurring = task.IsRecurring,
            IsActive = task.IsActive,
            DueDate = task.DueDate,
            AssignedToUserId = task.AssignedToUserId,
            CreatedAt = task.CreatedAt,
            RecurrencePattern = task.RecurrencePattern is not null
                ? new RecurrencePatternDto
                {
                    Id = task.RecurrencePattern.Id,
                    Type = task.RecurrencePattern.Type,
                    Interval = task.RecurrencePattern.Interval,
                    StartDate = task.RecurrencePattern.StartDate,
                    EndDate = task.RecurrencePattern.EndDate,
                    AssigneeUserIds = task.RecurrencePattern.Assignees
                        .OrderBy(a => a.Order)
                        .Select(a => a.UserId)
                        .ToList()
                }
                : null,
            Occurrences = task.Occurrences.Select(o => new OccurrenceDto
            {
                Id = o.Id,
                DueDate = o.DueDate,
                Status = o.Status,
                AssignedToUserId = o.AssignedToUserId,
                CompletedAt = o.CompletedAt,
                Notes = o.Notes
            }).ToList()
        };
    }
}
