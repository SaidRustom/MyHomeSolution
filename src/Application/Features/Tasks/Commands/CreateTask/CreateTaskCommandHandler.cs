using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(
    IApplicationDbContext dbContext,
    IPublisher publisher)
    : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new HouseholdTask
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsRecurring = request.IsRecurring,
            DueDate = request.DueDate,
            AssignedToUserId = request.AssignedToUserId
        };

        if (request.IsRecurring && request.RecurrenceType.HasValue && request.RecurrenceStartDate.HasValue)
        {
            var pattern = new RecurrencePattern
            {
                HouseholdTaskId = task.Id,
                Type = request.RecurrenceType.Value,
                Interval = request.Interval ?? 1,
                StartDate = request.RecurrenceStartDate.Value,
                EndDate = request.RecurrenceEndDate
            };

            if (request.AssigneeUserIds is { Count: > 0 })
            {
                for (var i = 0; i < request.AssigneeUserIds.Count; i++)
                {
                    pattern.Assignees.Add(new RecurrenceAssignee
                    {
                        RecurrencePatternId = pattern.Id,
                        UserId = request.AssigneeUserIds[i],
                        Order = i
                    });
                }
            }

            task.RecurrencePattern = pattern;
        }

        dbContext.HouseholdTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new TaskCreatedEvent(task.Id, task.Title), cancellationToken);

        return task.Id;
    }
}
